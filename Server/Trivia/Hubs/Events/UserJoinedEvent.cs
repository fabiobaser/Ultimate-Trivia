using System.Collections.Generic;

namespace Trivia.Hubs.Events
{
    public class UserJoinedEvent
    {
        public string NewUser { get; set; }
        public List<string> Usernames { get; set; }
    }
}