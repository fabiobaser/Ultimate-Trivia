using System.Collections.Concurrent;

namespace UltimateTrivia.Application
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