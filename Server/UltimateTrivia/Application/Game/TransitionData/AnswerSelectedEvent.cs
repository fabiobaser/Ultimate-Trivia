namespace UltimateTrivia.Application.Game.TransitionData
{
    public class AnswerSelectedEvent
    {
        public PlayerData Player { get; set; }
        public string AnswerId { get; set; }
    }
}