using System;
using System.Collections.Generic;
using System.Linq;

namespace UltimateTrivia.Application.Game
{
    public partial class Game
    {
        public class GameState
        {
            public class PlayerAnswer
            {
                public PlayerData Player { get; set; }
                public Answer Answer { get; set; }
                public DateTimeOffset AnswerGivenAt { get; set; }
            }

            public class Answer
            {
                public string Id { get; set; }
                public string Content { get; set; }
                public bool IsCorrect { get; set; }
            }
            
            public int MaxRounds { get; set; }
            public int AnswerDuration { get; set; }
            public int CurrentRoundNr { get; set; }
            public int CurrentQuestionNr { get; set; }
            public string CurrentCategory { get; set; }
            public PlayerData CurrentPlayer { get; set; }
            
            public string CurrentQuestion { get; set; }
            public List<PlayerAnswer> CurrentPlayerAnswers { get; set; } = new List<PlayerAnswer>();
            public DateTimeOffset? CurrentQuestionStartedAt { get; set; }
            public string CurrentCorrectAnswer => CurrentAnswers.First(a => a.IsCorrect).Content;
            public List<Answer> CurrentAnswers { get; set; } = new List<Answer>();
            
            
            public List<Player> Players { get; set; } = new List<Player>();
            public Dictionary<string, int> Points { get; set; } = new Dictionary<string, int>();


            public void NextRound()
            {
                CurrentRoundNr++;
                CurrentQuestionNr = 1;
                
            }

            public void NextQuestion()
            {
                
            }
        }
    }
}