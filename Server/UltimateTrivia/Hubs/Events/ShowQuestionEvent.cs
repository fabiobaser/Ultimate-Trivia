using System.Collections.Generic;
using UltimateTrivia.Hubs.Events.Models;

namespace UltimateTrivia.Hubs.Events
{
    public class ShowQuestionEvent
    {
        public int CurrentRoundNr { get; set; }
        public int MaxRoundNr { get; set; }
        public string Question { get; set; }
        public int CurrentQuestionNr { get; set; }
        public int MaxQuestionNr { get; set; }
        public List<Answer> Answers { get; set; }
    }
}