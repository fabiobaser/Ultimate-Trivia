using System;
using System.Threading;
using System.Threading.Tasks;

namespace StateMachine
{
    public class Event
    {
        public delegate Task EventHandler(object data, CancellationToken ct);
        
        public Enum Name { get; set; }
        private EventHandler OnEvent { get; set; }

        public Event(Enum name)
        {
            Name = name;
        }

        public Event WithAction(EventHandler eventHandler)
        {
            OnEvent = eventHandler;
            return this;
        }
    }
}