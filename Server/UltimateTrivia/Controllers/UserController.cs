using Microsoft.AspNetCore.Mvc;
using UltimateTrivia.Application;
using UltimateTrivia.BackgroundJobs;

namespace UltimateTrivia.Controllers
{
    public class UserController : BaseApiController
    {
        private readonly PlayerManager _playerManager;
        private readonly CleanOldLobbiesJob _job;
        private readonly LobbyManager _lobbyManager;

        public UserController(PlayerManager playerManager, CleanOldLobbiesJob job)
        {
            _playerManager = playerManager;
            _job = job;
        }
        
        
        [HttpGet()]
        public IActionResult GetAllLobbiesAsync()
        {
            return Ok(_playerManager.GetAllPlayers());
        }

        [HttpGet("remove-old-lobbies")]
        public IActionResult StartCleanOldLobbiesJob()
        {
            _job.Execute(null);
            return Ok();
        }
        
    }
}