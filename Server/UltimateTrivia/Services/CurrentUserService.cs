using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace UltimateTrivia.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }
        
        public string GetCurrentUser()
        {
            var identityUser = _httpContextAccessor.HttpContext?.User;

            if (identityUser == null)
            {
                return null;
            }
            
            var userId = identityUser.FindFirstValue(ClaimTypes.NameIdentifier);
            return userId;
        }
    }
}
