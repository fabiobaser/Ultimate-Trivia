using System.Collections.Generic;
using UltimateTrivia.Application;
using UltimateTrivia.Hubs.Events.Models;

namespace UltimateTrivia.Hubs.Events
{
    public class ShowCategoriesEvent
    {
        public List<Category> Categories { get; set; }
        public PlayerData CurrentPlayer { get; set; }
    }
}