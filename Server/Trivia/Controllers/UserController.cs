using Microsoft.AspNetCore.Mvc;
using Trivia.Application;
using Trivia.BackgroundJobs;

namespace Trivia.Controllers
{
    public class UserController : BaseController
    {
        private readonly UserManager _userManager;
        private readonly CleanOldLobbiesJob _job;
        private readonly LobbyManager _lobbyManager;

        public UserController(UserManager userManager, CleanOldLobbiesJob job)
        {
            _userManager = userManager;
            _job = job;
        }
        
        
        [HttpGet()]
        public IActionResult GetAllLobbiesAsync()
        {
            return Ok(_userManager.GetAllUsers());
        }

        [HttpGet("remove-old-lobbies")]
        public IActionResult StartCleanOldLobbiesJob()
        {
            _job.Execute(null);
            return Ok();
        }
        
    }
}