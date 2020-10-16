using System.Collections.Generic;
using UltimateTrivia.Application;

namespace UltimateTrivia.Hubs.Events
{
    public class ShowCategoriesEvent
    {
        public List<string> Categories { get; set; }
        public PlayerData CurrentPlayer { get; set; }
    }
}