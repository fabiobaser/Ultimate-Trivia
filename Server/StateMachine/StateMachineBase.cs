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
        private struct TransitionCommand
         
        {
            public Enum Command { get; }
            public object Data { get; }

            public TransitionCommand(Enum command, object data = null)
            {
                Command = command;
                Data = data;
            }
        }

        protected readonly ILogger Logger;
        
        protected State LastState { get; private set; }
        protected State CurrentState { get; private set; }
        private readonly List<State> _states = new List<State>();

        private readonly Timer _timeoutTimer;

        private readonly CancellationTokenSource _commandHandlerCts = new CancellationTokenSource();
        private readonly Task _commandHandler;
        
        private CancellationTokenSource _transitionCts = new CancellationTokenSource();
        
        private readonly BlockingCollection<TransitionCommand> _commandQueue = new BlockingCollection<TransitionCommand>();
        private readonly object _commandEnqueueLock = new object();
        private readonly object _commandDequeueLock = new object();

        protected StateMachineBase(ILogger logger)
        {
            Logger = logger;
            
            AddStates();
    
            CurrentState = _states.First(s => Equals(StateMachineBaseState.Idle, s.Name));

            _commandHandler = Task.Run(HandleCommands);

            _timeoutTimer = new Timer
            {
                AutoReset = false
            };
            _timeoutTimer.Elapsed += (sender, e) => Cancel("timeout exceeded");
        }

        protected virtual void AddStates()
        {
            AddState(StateMachineBaseState.Idle)
                .OnEnter(OnIdleEnter)
                .OnExit(OnIdleExit);
            AddState(StateMachineBaseState.Canceled)
                .OnEnter(OnCancelEnter)
                .OnExit(OnCancelExit)
                .On(StateMachineBaseCommand.Done)
                .Goto(StateMachineBaseState.Idle);
        }

        private async Task HandleCommands()
        {
            while (!_commandHandlerCts.Token.IsCancellationRequested)
            {
                try
                {
                    TransitionCommand cmd;
                    lock (_commandDequeueLock)
                    {
                        cmd = _commandQueue.Take(_transitionCts.Token);
                    }
                    await MoveNext(cmd.Command, _transitionCts.Token, cmd.Data);
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

        public void EnqueueTransition(Enum command, object data = null)
        {
            if (Equals(command, StateMachineBaseCommand.Cancel))
                throw new NotSupportedException("use \"StateMachine.Cancel()\" instead of enqueueing a cancel transition");

            lock (_commandEnqueueLock)
            {
                _commandQueue.Add(new TransitionCommand(command, data));
            }
        }

        protected async Task MoveNext(Enum command, CancellationToken cancellationToken, object data = null)
        {
            try
            {
                var transition = CurrentState.Transitions.FirstOrDefault(trans => Equals(trans.Command, command));
                if (transition == null) 
                    throw new ArgumentNullException($"{nameof(transition)}  --> transition: {command} from State: {CurrentState.Name} was not registered");

                cancellationToken.ThrowIfCancellationRequested();
                if(!Equals(command, StateMachineBaseCommand.Cancel))
                    await CurrentState.Exit(data, cancellationToken);

                _timeoutTimer.Stop();

                cancellationToken.ThrowIfCancellationRequested();

                LastState = CurrentState;
                CurrentState = _states.First(s => Equals(s.Name, transition.NextState));

                cancellationToken.ThrowIfCancellationRequested();

                StartTimeoutTimer();

                await CurrentState.Enter(data, cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();
            }
            catch (Exception e) when (!(e is OperationCanceledException))
            {
                Cancel(e.Message);
            }
            
            void StartTimeoutTimer()
            {
                if (CurrentState.Timeout <= 0)
                    return;

                _timeoutTimer.Interval = CurrentState.Timeout;
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

                _commandQueue.Add(new TransitionCommand(StateMachineBaseCommand.Cancel));

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
                .On(StateMachineBaseCommand.Cancel)
                .Goto(StateMachineBaseState.Canceled);
            
            _states.Add(state);
            return state;
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
            await MoveNext(StateMachineBaseCommand.Done, cancellationToken);
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
