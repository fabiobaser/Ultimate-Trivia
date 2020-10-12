using System.Collections.Concurrent;

namespace Trivia.Application
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