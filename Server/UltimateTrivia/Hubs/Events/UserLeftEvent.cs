using System.Collections.Generic;

namespace UltimateTrivia.Hubs.Events
{
    public class UserLeftEvent
    {
        public string LeavingUser { get; set; }
        public List<string> Usernames { get; set; }
    }
}