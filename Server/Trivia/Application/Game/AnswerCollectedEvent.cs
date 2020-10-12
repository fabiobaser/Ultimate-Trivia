namespace Trivia.Application.Game
{
    public class AnswerCollectedEvent
    {
        public string Username { get; set; }
        public string Answer { get; set; }
    }
}