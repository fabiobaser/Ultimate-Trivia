using System.Collections.Generic;

namespace UltimateTrivia.Hubs.Events
{
    public class HighlightCorrectAnswerEvent
    {
        public string CorrectAnswer { get; set; }
        public Dictionary<string, string> UserAnswers { get; set; }
    }
}