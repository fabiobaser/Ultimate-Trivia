using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UltimateTrivia.Services;

namespace UltimateTrivia.Controllers
{
    [Authorize]
    public class IdentityController : BaseApiController
    {
        private readonly ICurrentUserService _currentUserService;

        public IdentityController(ICurrentUserService currentUserService)
        {
            _currentUserService = currentUserService;
        }
        
        
        [HttpGet("identity")]
        public async Task<IActionResult> GetUser()
        {
            return Ok(_currentUserService.GetCurrentUser());
        }
    }
}