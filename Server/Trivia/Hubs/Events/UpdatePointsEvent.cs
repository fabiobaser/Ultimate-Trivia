using System.Collections.Generic;

namespace Trivia.Hubs.Events
{
    public class UpdatePointsEvent
    {
        public Dictionary<string, int> Points { get; set; }
    }
}