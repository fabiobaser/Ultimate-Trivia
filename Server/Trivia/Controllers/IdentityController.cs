using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Trivia.Services;

namespace Trivia.Controllers
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