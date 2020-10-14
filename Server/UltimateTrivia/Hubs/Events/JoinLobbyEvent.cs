using System.Collections.Generic;

namespace UltimateTrivia.Hubs.Events
{
    public class JoinLobbyEvent
    {
        public string LobbyId { get; set; }
        public List<string> Usernames { get; set; }
        public string Creator { get; set; }
    }
}