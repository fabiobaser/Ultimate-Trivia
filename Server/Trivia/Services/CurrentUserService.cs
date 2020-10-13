using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Trivia.Identity;

namespace Trivia.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }
        
        public User GetCurrentUser()
        {
            var identityUser = _httpContextAccessor.HttpContext?.User;

            if (identityUser == null)
            {
                return null;
            }
            
            var userId = identityUser.FindFirstValue(ClaimTypes.NameIdentifier);
            var name = identityUser.FindFirstValue("name");
            return new User
            {
                Id = userId,
                Name = name
            };

        }
    }
}
