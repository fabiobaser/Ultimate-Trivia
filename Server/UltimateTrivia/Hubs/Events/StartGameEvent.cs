﻿namespace UltimateTrivia.Hubs.Events
{
    public class StartGameEvent
    {
        public int Rounds { get; set; }
        public int AnswerDuration { get; set; }
    }
}