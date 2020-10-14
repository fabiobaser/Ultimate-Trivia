using System.Collections.Generic;

namespace UltimateTrivia.Hubs.Events
{
    public class UserJoinedEvent
    {
        public string NewUser { get; set; }
        public List<string> Usernames { get; set; }
    }
}