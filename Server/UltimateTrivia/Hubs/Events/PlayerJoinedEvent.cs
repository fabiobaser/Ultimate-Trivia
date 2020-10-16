using System.Collections.Generic;
using UltimateTrivia.Application;

namespace UltimateTrivia.Hubs.Events
{
    public class PlayerJoinedEvent
    {
        public PlayerData NewPlayer { get; set; }
        public List<PlayerData> Players { get; set; }
    }
}