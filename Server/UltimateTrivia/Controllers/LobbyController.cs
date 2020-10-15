using Microsoft.AspNetCore.Mvc;
using UltimateTrivia.Application;
using UltimateTrivia.BackgroundJobs;

namespace UltimateTrivia.Controllers
{
    public class LobbyController : BaseApiController
    {
        private readonly LobbyManager _lobbyManager;
        private readonly CleanOldLobbiesJob _job;

        public LobbyController(LobbyManager lobbyManager, CleanOldLobbiesJob job)
        {
            _lobbyManager = lobbyManager;
            _job = job;
        }
        
        
        [HttpGet()]
        public IActionResult GetAllLobbiesAsync()
        {
            return Ok(_lobbyManager.GetAllLobbies());
        }
        
        [HttpGet("{id}")]
        public IActionResult GetLobbyAsync(string lobbyId)
        {
            return Ok(_lobbyManager.GetAllLobbies());
        }
        
        
        [HttpGet("remove-old-lobbies")]
        public IActionResult StartCleanOldLobbiesJob()
        {
            _job.Execute(null);
            return Ok();
        }
        
    }
}