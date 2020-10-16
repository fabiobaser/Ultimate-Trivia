namespace UltimateTrivia.Application.Game.TransitionData
{
    public class CategorySelectedEvent
    {
        public string Category { get; set; }
        public PlayerData CurrentPlayer { get; set; }
    }
}