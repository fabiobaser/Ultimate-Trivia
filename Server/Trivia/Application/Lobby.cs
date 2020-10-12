using System;

namespace Trivia.Application
{
    public class Lobby : IDisposable
    {
        public string Id { get; set; }
        public Game.Game Game { get; set; }
        
        public Lobby(string lobbyId)
        {
            Id = lobbyId;
        }

        public void CreateGame(Game.Game game)
        {
            Game?.Dispose();

            Game = game;
            game.EnqueueTransition(Application.Game.Game.GameStateTransition.StartGame);
        }

        public void Dispose()
        {
            Game?.Dispose();
        }
    }
}