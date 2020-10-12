using System.Collections.Generic;

namespace Trivia.Hubs.Events
{
    public class UserLeftEvent
    {
        public string LeavingUser { get; set; }
        public List<string> Usernames { get; set; }
    }
}