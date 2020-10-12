using System.Linq;
using System.Threading.Tasks;
using Quartz;
using Trivia.Application;

namespace Trivia.BackgroundJobs
{
    public class CleanOldLobbiesJob : IJob
    {
        private readonly UserManager _userManager;
        private readonly LobbyManager _lobbyManager;

        public CleanOldLobbiesJob(UserManager userManager, LobbyManager lobbyManager)
        {
            _userManager = userManager;
            _lobbyManager = lobbyManager;
        }
        
        public Task Execute(IJobExecutionContext context)
        {
            var usedLobbies = _userManager.GetAllLobbyIds();

            var existingLobbies = _lobbyManager.GetAllLobbyNames();

            var unusedLobbies = existingLobbies.Except(usedLobbies).ToList();

            foreach (var unusedLobby in unusedLobbies)
            {
                _lobbyManager.DeleteLobby(unusedLobby);
            }
            return Task.CompletedTask;
        }
    }
}