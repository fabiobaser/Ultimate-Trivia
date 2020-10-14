using System.Collections.Concurrent;

namespace UltimateTrivia.Application
{
    public class LobbyStore
    {
        public ConcurrentDictionary<string, Lobby> Lobbies { get; }

        public LobbyStore()
        {
            Lobbies = new ConcurrentDictionary<string, Lobby>();
        }
    }
}