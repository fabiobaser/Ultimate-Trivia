using System.Collections.Generic;
using UltimateTrivia.Application;

namespace UltimateTrivia.Hubs.Events
{
    public class PlayerLeftEvent
    {
        public PlayerData LeavingPlayer { get; set; }
        public List<PlayerData> Players { get; set; }
    }
}