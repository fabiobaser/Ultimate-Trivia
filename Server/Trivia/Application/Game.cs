using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using StateMachine;

namespace Trivia.Application
{
    public class Game : StateMachineBase
    {
        public List<User> Users { get; set; }

        public Game(ILogger<Game> logger) : base(logger)
        {
        
        }
    }
}