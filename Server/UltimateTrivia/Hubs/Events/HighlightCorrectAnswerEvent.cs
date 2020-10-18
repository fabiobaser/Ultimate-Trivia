using System.Collections.Generic;
using UltimateTrivia.Application;
using UltimateTrivia.Hubs.Events.Models;

namespace UltimateTrivia.Hubs.Events
{
    public class HighlightCorrectAnswerEvent
    {
        public List<PlayerAnswer> Answers { get; set; }

        public class PlayerAnswer
        {
            public string Id { get; set; }
            public string Content { get; set; }
            public bool Correct { get; set; }
            public List<PlayerData> SelectedBy { get; set; }
        }
    }
}