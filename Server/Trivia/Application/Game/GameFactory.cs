using System;
using Microsoft.Extensions.DependencyInjection;

namespace Trivia.Application.Game
{
    public class GameFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public GameFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public Game Create(Action<GameConfiguration> configureOptions)
        {
            var options = new GameConfiguration();
            configureOptions?.Invoke(options);
            return ActivatorUtilities.CreateInstance<Game>(_serviceProvider, options);
        }
        
    }
}