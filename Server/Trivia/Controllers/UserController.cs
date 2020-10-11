using Microsoft.AspNetCore.Mvc;
using Trivia.Application;

namespace Trivia.Controllers
{
    public class UserController : BaseController
    {
        private readonly UserManager _userManager;
        private readonly LobbyManager _lobbyManager;

        public UserController(UserManager userManager)
        {
            _userManager = userManager;
        }
        
        
        [HttpGet()]
        public IActionResult GetAllLobbiesAsync()
        {
            return Ok(_userManager.GetAllUsers());
        }
        
    }
}