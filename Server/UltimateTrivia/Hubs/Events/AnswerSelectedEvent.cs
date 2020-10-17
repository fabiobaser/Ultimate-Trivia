using System.Collections.Generic;
using UltimateTrivia.Application;

namespace UltimateTrivia.Hubs.Events
{
    public class AnswerSelectedEvent
    {
        public PlayerData Player { get; set; }
        public List<PlayerData> RemainingPlayers { get; set; }
    }
}