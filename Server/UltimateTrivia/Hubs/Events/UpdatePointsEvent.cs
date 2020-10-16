using System.Collections.Generic;

namespace UltimateTrivia.Hubs.Events
{
    public class UpdatePointsEvent
    {
        public Dictionary<string, int> Points { get; set; } = new Dictionary<string, int>();
    }
}