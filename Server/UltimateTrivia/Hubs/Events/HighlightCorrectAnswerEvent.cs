using System.Collections.Generic;

namespace UltimateTrivia.Hubs.Events
{
    public class HighlightCorrectAnswerEvent
    {
        public List<Answer> Answers { get; set; }

        public class Answer
        {
            public string Content { get; set; }
            public bool Correct { get; set; }
            public List<string> SelectedBy { get; set; }
        }
    }
}