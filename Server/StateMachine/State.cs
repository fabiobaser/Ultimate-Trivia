using System;
using System.Collections.Generic;
using System.Threading;
using Utils;

namespace StateMachine
{
    public class State
    {
        public delegate void StateEventHandler(object data, CancellationToken ct);
        
        public Enum Name { get; }
        private event StateEventHandler OnStateEnter;
        private event StateEventHandler OnStateExit;
        public List<Transition> Transitions = new List<Transition>();
        public double Timeout { get; private set; }

        public State(Enum name)
        {
            Name = name;
        }

        public State OnEnter(params StateEventHandler[] events)
        {
            foreach (var e in events)
            {
                OnStateEnter += e;
            }
            return this;
        }

        public State OnExit(params StateEventHandler[] events)
        {
            foreach (var e in events)
            {
                OnStateExit += e;
            }
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

        public void Enter(object data, CancellationToken ct)
        {
            OnStateEnter?.Invoke(data, ct);
        }

        public void Exit(object data, CancellationToken ct)
        {
            OnStateExit?.Invoke(data, ct);
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