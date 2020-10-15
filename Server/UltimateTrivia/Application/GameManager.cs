using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using UltimateTrivia.Application.Game;
using UltimateTrivia.Exceptions;

namespace UltimateTrivia.Application
{
    public class GameManager : IDisposable
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<GameManager> _logger;
        private readonly ConcurrentDictionary<string, GameHost> _gameHosts;

        public GameManager(IServiceProvider serviceProvider, ILogger<GameManager> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _gameHosts = new ConcurrentDictionary<string, GameHost>();
        }

        public List<string> GetAllGames()
        {
            return _gameHosts.Keys.ToList();
        }

        public bool IsGameInProgress(string gameId)
        {
            if (_gameHosts.TryGetValue(gameId, out var host))
            {
                return host.Game.IsRunning;
            }
            throw new NotFoundException($"Game {gameId} doesnt exist");
        }
        
        public string CreateGame(Action<GameConfiguration> configureOptions)
        {
            var host = new GameHost(_serviceProvider);

            var gameId = host.CreateGame(configureOptions);

            _gameHosts[gameId] = host;

            return gameId;
        }

        public void DeleteGame(string gameId)
        {
            _logger.LogDebug("deleting game {gameId}", gameId);
            _gameHosts[gameId]?.Dispose();
            if (_gameHosts.TryRemove(gameId, out _))
            {
                throw new ApplicationException($"failed to delete game {gameId}");
            }
        }

        public void PassEventToGame(string gameId, Game.Game.EGameStateTransition transition, object data = null)
        {
            var host = _gameHosts[gameId];
            host.Game.EnqueueTransition(transition, data);
        }

        public void Dispose()
        {
            foreach (var (gameId, host) in _gameHosts)
            {
                host?.Dispose();
            }
        }
    }
}