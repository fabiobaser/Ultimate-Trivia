using System.Collections.Generic;
using UltimateTrivia.Hubs.Events.Models;

namespace UltimateTrivia.Hubs.Events
{
    public class ShowQuestionEvent
    {
        public string Question { get; set; }
        public int QuestionNr { get; set; }
        public List<Answer> Answers { get; set; }
    }
}