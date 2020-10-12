using System.Collections.Concurrent;

namespace Trivia.Application
{
    public class UserStore
    {
        public ConcurrentDictionary<string, User> Users { get; }

        public UserStore()
        {
            Users = new ConcurrentDictionary<string, User>();
        }
    }
}