using System.Collections.Generic;

namespace Trivia.Hubs.Events
{
    public class ShowCategoriesEvent
    {
        public List<string> Categories { get; set; }
        public string Username { get; set; }
    }
}