using System.Collections.Concurrent;

namespace Trivia.Application
{
    public class PlayerStore
    {
        public ConcurrentDictionary<string, Player> Players { get; }

        public PlayerStore()
        {
            Players = new ConcurrentDictionary<string, Player>();
        }
    }
}