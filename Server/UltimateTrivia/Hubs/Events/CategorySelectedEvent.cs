using UltimateTrivia.Application;
using UltimateTrivia.Hubs.Events.Models;

namespace UltimateTrivia.Hubs.Events
{
    public class CategorySelectedEvent
    {
        public Category Category { get; set; }
        public PlayerData Player { get; set; }
    }
}