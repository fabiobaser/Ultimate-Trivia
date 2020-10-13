﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Trivia.Application.Game;

namespace Trivia.Application
{
    public class GameManager : IDisposable
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<GameManager> _logger;
        private readonly ConcurrentDictionary<string, GameHost> _games;

        public GameManager(IServiceProvider serviceProvider, ILogger<GameManager> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _games = new ConcurrentDictionary<string, GameHost>();
        }

        public List<string> GetAllGames()
        {
            return _games.Keys.ToList();
        }
        
        public string CreateGame(Action<GameConfiguration> configureOptions)
        {
            var host = new GameHost(_serviceProvider);

            var gameId = host.CreateGame(configureOptions);

            _games[gameId] = host;

            return gameId;
        }

        public void DeleteGame(string gameId)
        {
            _logger.LogDebug("deleting game {gameId}", gameId);
            _games[gameId]?.Dispose();
            if (_games.TryRemove(gameId, out _))
            {
                throw new ApplicationException($"failed to delete game {gameId}");
            }
        }

        public void PassEventToGame(string gameId, Game.Game.GameStateTransition transition, object data = null)
        {
            var game = _games[gameId];
            game.EnqueueTransition(transition, data);
        }

        public void Dispose()
        {
            foreach (var (gameId, host) in _games)
            {
                host?.Dispose();
            }
        }
    }
}