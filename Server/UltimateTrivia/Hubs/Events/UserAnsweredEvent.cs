using System.Collections.Generic;

namespace UltimateTrivia.Hubs.Events
{
    public class UserAnsweredEvent
    {
        public string Username { get; set; }
        public List<string> RemainingUsers { get; set; }
    }
}