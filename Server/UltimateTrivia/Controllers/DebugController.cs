using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace UltimateTrivia.Controllers
{
    [ApiController]
    [Route("")]
    public class DebugController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;

        public DebugController(UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
        }

        [HttpGet("api/v1/debug/exception")]
        public async Task<IActionResult> ThrowApiException()
        {
            throw new ApplicationException("sumthing went wung");
        }
        
        [HttpGet("debug/exception")]
        public async Task<IActionResult> ThrowException()
        {
            throw new ApplicationException("sumthing went wung");
        }
        
        [Authorize()]
        [HttpGet("debug")]
        public async Task<IActionResult> Authenticate()
        {

            var user = await _userManager.GetUserAsync(User);
            
            return Ok();
        }
    }
}