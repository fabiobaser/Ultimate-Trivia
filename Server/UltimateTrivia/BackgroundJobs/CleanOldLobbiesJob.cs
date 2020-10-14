using System.Linq;
using System.Threading.Tasks;
using Quartz;
using UltimateTrivia.Application;

namespace UltimateTrivia.BackgroundJobs
{
    public class CleanOldLobbiesJob : IJob
    {
        private readonly PlayerManager _playerManager;
        private readonly LobbyManager _lobbyManager;

        public CleanOldLobbiesJob(PlayerManager playerManager, LobbyManager lobbyManager)
        {
            _playerManager = playerManager;
            _lobbyManager = lobbyManager;
        }
        
        public Task Execute(IJobExecutionContext context)
        {
            var usedLobbies = _playerManager.GetAllLobbyIds();

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