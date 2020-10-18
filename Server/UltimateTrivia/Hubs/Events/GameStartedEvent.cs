using System.Collections.Generic;
using UltimateTrivia.Application;
using UltimateTrivia.Application.Game;
using UltimateTrivia.Hubs.Events.Models;

namespace UltimateTrivia.Hubs.Events
{
    public class GameStartedEvent
    {
        public int MaxRoundNr { get; set; }
        public int CurrentRoundNr { get; set; }
        public int MaxQuestionNr { get; set; }
        public int CurrentQuestionNr { get; set; }
        public PlayerData CurrentPlayer { get; set; }
        public Game.GameState.Category CurrentCategory { get; set; }
        public string CurrentQuestion { get; set; }
        public List<Answer> CurrentAnswers { get; set; }
        public Dictionary<string, int> Points { get; set; }
    }
}