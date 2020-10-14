using Microsoft.AspNetCore.Mvc;
using UltimateTrivia.Application;

namespace UltimateTrivia.Controllers
{
    public class GameController : BaseApiController
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