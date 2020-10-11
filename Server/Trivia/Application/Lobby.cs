using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Trivia.Application
{
    public class Lobby
    {
        public string Id { get; set; }
        public Game Game { get; set; }
        
        public Lobby(string lobbyId)
        {
            Id = lobbyId;
        }

        public void CreateGame()
        {
            
        }
    }
}