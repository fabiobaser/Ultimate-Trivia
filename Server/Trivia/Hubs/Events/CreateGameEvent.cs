namespace Trivia.Hubs.Events
{
    public class CreateGameEvent
    {
        public int Rounds { get; set; }
        public int RoundDuration { get; set; }
    }
}