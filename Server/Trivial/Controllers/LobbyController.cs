using Microsoft.AspNetCore.Mvc;
using Trivial.Application;

namespace Trivial.Controllers
{
    public class LobbyController : BaseController
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