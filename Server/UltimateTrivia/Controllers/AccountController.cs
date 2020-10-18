using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UltimateTrivia.Database.Game;
using UltimateTrivia.Services;

namespace UltimateTrivia.Controllers
{
    [Authorize]
    public class AccountController : BaseApiController
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly UserRepository _userRepository;

        public AccountController(ICurrentUserService currentUserService, UserRepository userRepository)
        {
            _currentUserService = currentUserService;
            _userRepository = userRepository;
        }
        
        
        [HttpGet("identity")]
        public async Task<IActionResult> GetUserIdentity()
        {
            return Ok(_currentUserService.GetCurrentUserIdentity());
        }

        [HttpGet("user")]
        public async Task<IActionResult> GetUser()
        {
            return Ok(await _userRepository.GetUserByIdentityId(_currentUserService.GetCurrentUserIdentity()));
        }

        [HttpGet("login-callback")]
        public async Task<IActionResult> LoginCallback()
        {
            await _userRepository.CreateUser();
            return Ok();
        }
    }
}