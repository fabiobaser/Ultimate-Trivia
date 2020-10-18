using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using UltimateTrivia.Database.Game.Entities;
using UltimateTrivia.Exceptions;
using UltimateTrivia.Services;

namespace UltimateTrivia.Database.Game
{
    public class UserRepository
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly ApplicationDbContext _dbContext;

        public UserRepository(ICurrentUserService currentUserService, ApplicationDbContext dbContext)
        {
            _currentUserService = currentUserService;
            _dbContext = dbContext;
        }

        public async Task CreateUser()
        {
            if (!_dbContext.Users.Any(u => u.IdentityId == _currentUserService.GetCurrentUserIdentity()))
            {
                var user = new User
                {
                    Id = Guid.NewGuid().ToString(),
                    IdentityId = _currentUserService.GetCurrentUserIdentity()
                };

                await _dbContext.Users.AddAsync(user);
                await _dbContext.SaveChangesAsync();
            }
        }

        public async Task<User> GetUserById(string id)
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                throw new NotFoundException($"user {id} doesnt exist");
            }

            return user;
        }
        
        public async Task<User> GetUserByIdentityId(string id)
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.IdentityId == id);

            return user;
        }

        public async Task UpdateUser(User user)
        {
            _dbContext.Users.Update(user);
            await _dbContext.SaveChangesAsync();
        }
    }
}