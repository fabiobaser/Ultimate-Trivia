using System;

namespace Trivia.Application
{
    public class Lobby
    {
        public string Id { get; set; }
        public string Creator { get; set; }
        public string GameId { get; set; }
        
        public Lobby(string lobbyId, string creator)
        {
            Creator = creator;
            Id = lobbyId;
        }

        public void ConnectToGame(string gameId)
        {
            GameId = gameId;
        }
    }
}