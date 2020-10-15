using Microsoft.AspNetCore.Mvc;
using UltimateTrivia.Application;
using UltimateTrivia.BackgroundJobs;

namespace UltimateTrivia.Controllers
{
    public class PlayerController : BaseApiController
    {
        private readonly PlayerManager _playerManager;

        public PlayerController(PlayerManager playerManager)
        {
            _playerManager = playerManager;
        }
        
        
        [HttpGet()]
        public IActionResult GetAllPlayersAsync()
        {
            return Ok(_playerManager.GetAllPlayers());
        }
    }
}