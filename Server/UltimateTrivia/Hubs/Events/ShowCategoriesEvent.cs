using System.Collections.Generic;

namespace UltimateTrivia.Hubs.Events
{
    public class ShowCategoriesEvent
    {
        public List<string> Categories { get; set; }
        public string Username { get; set; }
    }
}