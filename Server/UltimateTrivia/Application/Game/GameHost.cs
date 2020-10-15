using System;
using Microsoft.Extensions.DependencyInjection;

namespace UltimateTrivia.Application.Game
{
    public class GameHost : IDisposable
    {
        private IServiceScope _scope;
        public Game Game { get; private set; }

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

        public void Dispose()
        {
            Game?.Dispose();
            _scope?.Dispose();
        }
    }
}