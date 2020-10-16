using UltimateTrivia.Application;

namespace UltimateTrivia.Hubs.Events
{
    public class CategorySelectedEvent
    {
        public string Category { get; set; }
        public PlayerData Player { get; set; }
    }
}