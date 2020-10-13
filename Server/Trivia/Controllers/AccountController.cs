using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;

namespace Trivia.Controllers
{
    [Route("[controller]")]
    public class AccountController : Controller
    {
        public AccountController()
        {
        }

        [HttpGet("login")]
        public async Task<IActionResult> Login(string redirectUri = "/")
        {
            var props = new AuthenticationProperties
            {
                RedirectUri = Url.Action(nameof(LoginCallback)),
                Items =
                {
                    ["originalRedirectUri"] = redirectUri
                }
            };
            return Challenge(props);
        }

        [HttpGet]
        public async Task<IActionResult> LoginCallback()
        {
            var result = await HttpContext.AuthenticateAsync();
            if (!result.Succeeded)
            {
                throw new Exception("oops");    // TODO: better error
            }

            // create Application User object if it doesnt exist yet
            
            return Redirect(result.Properties.Items["originalRedirectUri"]);
        }

        [HttpGet("logout")]
        public async Task<IActionResult> Logout()
        {
            return SignOut(new AuthenticationProperties
            {
                RedirectUri = "/"
            }, CookieAuthenticationDefaults.AuthenticationScheme, GoogleDefaults.AuthenticationScheme);    // TODO: dynamically adapt to multiple identity providers
        }
        
       
       
    }
}