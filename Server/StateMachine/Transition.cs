using System;
using Utils;

namespace StateMachine
{
    public class Transition
    {
        public State State { get; }
        public Enum Command { get; }
        public Enum NextState { get; set; }

        public Transition(State state, Enum command)
        {
            State = state;
            Command = command;
        }

        public State Goto(Enum state)
        {
            if (state.GetType() != typeof(StateMachineBaseState) && Enum.IsDefined(typeof(StateMachineBaseState), state.ToString()))
                throw new Exception($"\"{state}\" is already defined in {nameof(StateMachineBaseState)}!");

            NextState = state;
            return State;
        }
    }
}