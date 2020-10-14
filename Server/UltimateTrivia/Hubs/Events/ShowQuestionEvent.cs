using System.Collections.Generic;

namespace UltimateTrivia.Hubs.Events
{
    public class ShowQuestionEvent
    {
        public string Question { get; set; }
        public List<string> Answers { get; set; }
    }
}