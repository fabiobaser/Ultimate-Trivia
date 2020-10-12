using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Trivia.Application.Game;

namespace Trivia.HostedServices
{
    public class GameManager : IDisposable
    {
        public class GameHost : IDisposable
        {
            private IServiceScope _scope;
            private Game Game { get; set; }

            public GameHost(IServiceProvider serviceProvider)
            {
                _scope = serviceProvider.CreateScope();
            }
            
            public string CreateGame(Action<GameConfiguration> configureOptions)
            {
                var options = new GameConfiguration();
                configureOptions?.Invoke(options);
                Game = ActivatorUtilities.CreateInstance<Game>(_scope.ServiceProvider, options);
                return Game.Id;
            }

            public void EnqueueTransition(Enum command, object data = null) => Game.EnqueueTransition(command, data);

            public void Dispose()
            {
                Game?.Dispose();
                _scope?.Dispose();
            }
        }
        
        private readonly IServiceProvider _serviceProvider;
        private readonly ConcurrentDictionary<string, GameHost> _games;

        public GameManager(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _games = new ConcurrentDictionary<string, GameHost>();
        }

        public string CreateGame(Action<GameConfiguration> configureOptions)
        {
            var host = new GameHost(_serviceProvider);

            var gameId = host.CreateGame(configureOptions);

            _games[gameId] = host;

            return gameId;
        }

        public void PassEventToGame(string gameId, Game.GameStateTransition transition, object data = null)
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