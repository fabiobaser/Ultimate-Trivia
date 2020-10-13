using Microsoft.AspNetCore.Mvc;
using Trivia.Application;

namespace Trivia.Controllers
{
    public class LobbyController : BaseApiController
    {
        private readonly LobbyManager _lobbyManager;

        public LobbyController(LobbyManager lobbyManager)
        {
            _lobbyManager = lobbyManager;
        }
        
        
        [HttpGet()]
        public IActionResult GetAllLobbiesAsync()
        {
            return Ok(_lobbyManager.GetAllLobbyNames());
        }
        
    }
}