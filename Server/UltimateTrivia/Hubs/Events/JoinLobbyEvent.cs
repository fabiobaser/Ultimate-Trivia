using System.Collections.Generic;
using UltimateTrivia.Application;

namespace UltimateTrivia.Hubs.Events
{
    public class JoinLobbyEvent
    {
        public string LobbyId { get; set; }
        public PlayerData Player { get; set; }
        public List<PlayerData> Players { get; set; }
        public string CreatorId { get; set; }
    }
}