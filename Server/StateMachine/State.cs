using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Utils;

namespace StateMachine
{
    public class State
    {
        public delegate Task StateEventHandler(object data, CancellationToken ct);
        
        public Enum Name { get; }
        public List<Transition> Transitions = new List<Transition>();
        public TimeSpan TimeoutDuration { get; private set; }
        public bool HasCustomerTimeoutHandler => OnStateTimeout != null;
        
        private StateEventHandler OnStateEnter;
        private StateEventHandler OnStateExit;
        private StateEventHandler OnStateTimeout;
        
        public State(Enum name)
        {
            Name = name;
        }

        public State OnEnter(StateEventHandler onEnter)
        {
            OnStateEnter = onEnter;
            
            return this;
        }

        public State OnExit(StateEventHandler onExit)
        {
            OnStateExit = onExit;
            return this;
        }

        public State OnTimeout(StateEventHandler onTimeout)
        {
            OnStateTimeout = onTimeout;
            return this;
        }

        public State TimeoutAfter(TimeSpan timeout)
        {
            TimeoutDuration = timeout;
            return this;
        }

        public async Task Enter(object data, CancellationToken ct)
        {
            if (OnStateEnter != null) await OnStateEnter(data, ct);
        }

        public async Task Exit(object data, CancellationToken ct)
        {
            if (OnStateExit != null) await OnStateExit(data, ct);
        }

        public async Task Timeout(object data, CancellationToken ct)
        {
            if (OnStateTimeout != null) await OnStateTimeout(data, ct);
        }

        public Transition On(Enum command)
        {
            if (command.GetType() != typeof(StateMachineBaseTransition) && Enum.IsDefined(typeof(StateMachineBaseTransition), command.ToString()))
                throw new Exception($"\"{command}\" is already defined in {nameof(StateMachineBaseTransition)}! please use this one and remove it from {command.GetType()}");

            if (Transitions.Exists(trans => trans.Command.Equals(command)))
                throw new Exception($"A transition for {Name} --> {command} already exists");

            var transition = new Transition(this, command);
            Transitions.Add(transition);
            return transition;
        }
    }
}