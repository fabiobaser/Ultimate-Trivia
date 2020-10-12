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
        private StateEventHandler OnStateEnter;
        private StateEventHandler OnStateExit;
        public List<Transition> Transitions = new List<Transition>();
        public double Timeout { get; private set; }

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

        public State TimeoutAfter(TimeSpan timeout)
        {
            return TimeoutAfter(timeout.TotalMilliseconds);
        }
        
        public State TimeoutAfter(double timeout)
        {
            Timeout = timeout;
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

        public Transition On(Enum command)
        {
            if (command.GetType() != typeof(StateMachineBaseCommand) && Enum.IsDefined(typeof(StateMachineBaseCommand), command.ToString()))
                throw new Exception($"\"{command}\" is already defined in {nameof(StateMachineBaseCommand)}! please use this one and remove it from {command.GetType()}");

            if (Transitions.Exists(trans => trans.Command.Equals(command)))
                throw new Exception($"A transition for {Name} --> {command} already exists");

            var transition = new Transition(this, command);
            Transitions.Add(transition);
            return transition;
        }
    }
}