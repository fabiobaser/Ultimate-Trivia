using System.Collections.Generic;

namespace Trivia.Hubs.Events
{
    public class HighlightCorrectAnswerEvent
    {
        public string CorrectAnswer { get; set; }
        public Dictionary<string, string> UserAnswers { get; set; }
    }
}