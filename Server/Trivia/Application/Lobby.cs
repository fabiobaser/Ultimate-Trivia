using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Trivia.Application
{
    public class Lobby
    {
        public string Name { get; set; }
        public Game Game { get; set; }
        public ConcurrentBag<User> Users { get; set; }
        
        public Lobby(string lobbyName)
        {
            Name = lobbyName;
            Users = new ConcurrentBag<User>();
        }

        public async Task JoinAsync(User user)
        {
            Users.Add(user);
        }

        public void CreateGame()
        {
            
        }
    }
}