using System.Collections.Generic;

namespace Trivia.Hubs.Events
{
    public class ShowFinalResultEvent
    {
        public Dictionary<string, int> Points { get; set; }
    }
}