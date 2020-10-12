using Microsoft.AspNetCore.Mvc;
using Trivia.HostedServices;

namespace Trivia.Controllers
{
    public class GameController : BaseController
    {
        private readonly GameManager _gameManager;

        public GameController(GameManager gameManager)
        {
            _gameManager = gameManager;
        }
        
        [HttpGet()]
        public IActionResult GetAllLobbiesAsync()
        {
            return Ok(_gameManager.GetAllGames());
        }
        
    }
}