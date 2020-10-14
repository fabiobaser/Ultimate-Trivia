using System.Collections.Generic;

namespace UltimateTrivia.Hubs.Events
{
    public class ShowFinalResultEvent
    {
        public Dictionary<string, int> Points { get; set; }
    }
}