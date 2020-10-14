using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace UltimateTrivia.Controllers
{
    [AllowAnonymous]
    [ApiController]
    [Route("")]
    public class DebugController : ControllerBase
    {
        public DebugController()
        {
            
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
    }
}