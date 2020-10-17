using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Utils;
using Timer = System.Timers.Timer;

namespace StateMachine
{
    public abstract class StateMachineBase : IDisposable
        
    {
        private struct StateMachineCommand
        {
            public enum StateMachineCommandType
            {
                Timout,
                Transition,
                Event
            }
            
            public StateMachineCommandType CommandType { get; }
            public Enum Command { get; }
            public object Data { get; }

            public StateMachineCommand(StateMachineCommandType commandType, Enum command, object data = null)
            {
                CommandType = commandType;
                Command = command;
                Data = data;
            }
            
            public static StateMachineCommand Transition(Enum command, object data = null) => new StateMachineCommand(StateMachineCommandType.Transition, command, data);
            public static StateMachineCommand Event(Enum command, object data = null) => new StateMachineCommand(StateMachineCommandType.Event, command, data);
            public static StateMachineCommand Timeout(object data) => new StateMachineCommand(StateMachineCommandType.Timout, null, data);
        }

        protected readonly ILogger Logger;

        public bool IsRunning => !Equals(CurrentState.Name, StateMachineBaseState.Idle);
        protected State LastState { get; private set; }
        protected State CurrentState { get; private set; }
        private readonly List<State> _states = new List<State>();
        private readonly List<Event> _events = new List<Event>();

        private readonly Timer _timeoutTimer;
        private Enum TimeoutState;

        private readonly CancellationTokenSource _commandHandlerCts = new CancellationTokenSource();
        private readonly Task _commandHandler;
        
        private CancellationTokenSource _transitionCts = new CancellationTokenSource();
        
        private readonly BlockingCollection<StateMachineCommand> _commandQueue = new BlockingCollection<StateMachineCommand>();
        private readonly object _commandEnqueueLock = new object();
        private readonly object _commandDequeueLock = new object();

        protected StateMachineBase(ILogger logger)
        {
            Logger = logger;
            
            Configure();
    
            CurrentState = _states.First(s => Equals(StateMachineBaseState.Idle, s.Name));

            _commandHandler = Task.Run(HandleCommands);

            _timeoutTimer = new Timer
            {
                AutoReset = false
            };
            _timeoutTimer.Elapsed += (sender, e) => EnqueueTimeout();
        }

        protected virtual void Configure()
        {
            AddState(StateMachineBaseState.Idle)
                .OnEnter(OnIdleEnter)
                .OnExit(OnIdleExit);
            AddState(StateMachineBaseState.Canceled)
                .OnEnter(OnCancelEnter)
                .OnExit(OnCancelExit)
                .On(StateMachineBaseTransition.Done)
                .Goto(StateMachineBaseState.Idle);
        }

        private async Task HandleCommands()
        {
            while (!_commandHandlerCts.Token.IsCancellationRequested)
            {
                try
                {
                    StateMachineCommand cmd;
                    lock (_commandDequeueLock)
                    {
                        cmd = _commandQueue.Take(_transitionCts.Token);
                    }

                    switch (cmd.CommandType)
                    {
                        case StateMachineCommand.StateMachineCommandType.Transition:
                            await MoveNext(cmd.Command, _transitionCts.Token, cmd.Data);
                            break;
                        case StateMachineCommand.StateMachineCommandType.Event:
                            await HandleEvent(cmd.Command, _transitionCts.Token, cmd.Data);
                            break;
                        case StateMachineCommand.StateMachineCommandType.Timout:
                            await HandleTimeout(_transitionCts.Token, cmd.Data);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                catch(OperationCanceledException e)
                {
                    // ignore transition cancelled exception
                    // StateMachine.Cancel() will clear the queue and enqueue a cancel transition
                }
                catch(Exception e)
                {
                    Logger.LogError(e, "an error occured");
                }
            }
        }

        public void EnqueueTransition(Enum transition, object data = null)
        {
            if (Equals(transition, StateMachineBaseTransition.Cancel))
                throw new NotSupportedException("use \"StateMachine.Cancel()\" instead of enqueueing a cancel transition");

            lock (_commandEnqueueLock)
            {
                _commandQueue.Add(StateMachineCommand.Transition(transition, data));
            }
        }
        
        public void EnqueueEvent(Enum evt, object data = null)
        {
            lock (_commandEnqueueLock)
            {
                _commandQueue.Add(StateMachineCommand.Event(evt, data));
            }
        }
        
        public void EnqueueTimeout(object data = null)
        {
            lock (_commandEnqueueLock)
            {
                _commandQueue.Add(StateMachineCommand.Timeout(data));
            }
        }

        protected async Task HandleEvent(Enum evt, CancellationToken cancellationToken, object data = null)
        {
            var @event = _events.FirstOrDefault(e => Equals(e.Name, evt));

            if (@event == null)
            {
                Logger.LogError("event {eventName} is not defined", evt);
                return;
            }
            
            await @event.Execute(data, cancellationToken);

        }
        
        protected async Task HandleTimeout(CancellationToken cancellationToken, object data = null)
        {
            if (!Equals(CurrentState.Name, TimeoutState))
            {
                // prevent racecondition. timeout exceeded, but state already changed at the same time
                return;
            }

            if (CurrentState.HasCustomerTimeoutHandler)
            {
                await CurrentState.Timeout(data, cancellationToken);
            }
            else
            {
                Cancel("timeout exceeded");
            }
        }
        
        protected async Task MoveNext(Enum command, CancellationToken cancellationToken, object data = null)
        {
            try
            {
                var transition = CurrentState.Transitions.FirstOrDefault(trans => Equals(trans.Command, command));
                if (transition == null)
                {
                    Logger.LogError($"{nameof(transition)}  --> transition: {command} from State: {CurrentState.Name} was not registered");
                    return;
                }

                cancellationToken.ThrowIfCancellationRequested();
                if(!Equals(command, StateMachineBaseTransition.Cancel))
                    await CurrentState.Exit(data, cancellationToken);

                _timeoutTimer.Stop();

                cancellationToken.ThrowIfCancellationRequested();

                LastState = CurrentState;
                CurrentState = _states.First(s => Equals(s.Name, transition.NextState));

                cancellationToken.ThrowIfCancellationRequested();

                StartTimeoutTimer();

                Logger.LogTrace("moved from state {lastState} to {state} with {transition} Transition", LastState.Name, CurrentState.Name, command);
                await CurrentState.Enter(data, cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();
            }
            catch (Exception e) when (!(e is OperationCanceledException))
            {
                Cancel(e.Message);
            }
            
            void StartTimeoutTimer()
            {
                if (CurrentState.TimeoutDuration.TotalMilliseconds <= 0)
                    return;

                TimeoutState = CurrentState.Name;
                _timeoutTimer.Interval = CurrentState.TimeoutDuration.TotalMilliseconds;
                _timeoutTimer.Start();
            }
        }

        public void Cancel(string reason = "undefined")
        {
            Logger.LogError($"cancelling Statemaschine with reason: {reason}");
            _transitionCts.Cancel();

            lock (new[] { _commandEnqueueLock, _commandDequeueLock })
            {
                while (_commandQueue.Count > 0)
                    _commandQueue.Take();

                _commandQueue.Add(StateMachineCommand.Transition(StateMachineBaseTransition.Cancel));

                _transitionCts = new CancellationTokenSource();
            }
        }

        protected State AddState(Enum name)
        {
            if(_states.Exists(s => Equals(s.Name, name)))
                throw new Exception($"State {name} already exists");
            if (name.GetType() != typeof(StateMachineBaseState) && Enum.IsDefined(typeof(StateMachineBaseState), name.ToString()))
                throw new Exception($"\"{name}\" is already defined in {nameof(StateMachineBaseState)}!");

            var state = new State(name)
                .On(StateMachineBaseTransition.Cancel)
                .Goto(StateMachineBaseState.Canceled);
            
            _states.Add(state);
            return state;
        }
        
        protected Event AddEvent(Enum name)
        {
            var evt = new Event(name);
            _events.Add(evt);
            return evt;
        }

        protected State GetState(Enum name)
        {
            var state = _states.FirstOrDefault(s => Equals(s.Name, name));
            if(state == null) throw new NullReferenceException(nameof(state));
            return state;
        }

        protected virtual Task OnIdleEnter(object data, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        protected virtual Task OnIdleExit(object data, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        protected virtual async Task OnCancelEnter(object data, CancellationToken cancellationToken)
        {
            await MoveNext(StateMachineBaseTransition.Done, cancellationToken);
        }

        protected virtual Task OnCancelExit(object data, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public virtual void Dispose()
        {
            _timeoutTimer?.Stop();
            _timeoutTimer?.Dispose();

            _commandHandlerCts?.Cancel();
            _transitionCts?.Cancel();

            _commandHandler?.Wait();
            _commandHandler?.Dispose();

            _commandHandlerCts?.Dispose();
            _transitionCts?.Dispose();
        }
    }
}