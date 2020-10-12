using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Trivia.Hubs;
using Trivia.Hubs.Events;
using Trivia.Services;

namespace Trivia.Application
{
    public class LobbyManager
    {
        private readonly IInviteCodeGenerator _inviteCodeGenerator;
        private readonly UserManager _userManager;
        private readonly IHubContext<TriviaGameHub> _hubContext;
        private readonly ConcurrentDictionary<string, Lobby> _lobbies;

        public LobbyManager(IInviteCodeGenerator inviteCodeGenerator, UserManager userManager, IHubContext<TriviaGameHub> hubContext)
        {
            _inviteCodeGenerator = inviteCodeGenerator;
            _userManager = userManager;
            _hubContext = hubContext;

            _lobbies = new ConcurrentDictionary<string, Lobby>();
        }

        public IEnumerable<string> GetAllLobbyNames()
        {
            return _lobbies.Keys;
        }

        public void DeleteLobby(string lobbyId)
        {
            if (_lobbies.TryRemove(lobbyId, out _))
            {
                throw new ApplicationException($"lobby {lobbyId} couldnt be deleted");
            }
        }
        
        public async Task<Lobby> CreateLobbyAsync()
        {
            string lobbyId;
            do
            {
                lobbyId = _inviteCodeGenerator.GenerateCode();
            } 
            while (_lobbies.ContainsKey(lobbyId));
            
            var lobby = new Lobby(lobbyId);
            _lobbies[lobbyId] = lobby;

            return lobby;
        }
    
        public async Task JoinLobbyAsync(string lobbyId, string username, string connectionId)
        {
            if (!_lobbies.ContainsKey(lobbyId))
            {
                throw new ApplicationException("lobby doesnt exist");
            }

            _userManager.JoinLobby(connectionId, lobbyId);

            var usersInLobby = _userManager.GetUsersInLobby(lobbyId);

            await _hubContext.Clients.Group(lobbyId).SendAsync(ClientCallNames.UserJoinedLobby, new UserJoinedEvent
            {
                NewUser = username,
                Usernames = usersInLobby.Select(u => u.Name).ToList()
            });
        }

        public async Task LeaveLobbyAsync(string connectionId)
        {
            var user = _userManager.GetUserByConnectionId(connectionId);
            if (user.LobbyId == null)
            {
                throw new ApplicationException("user is not in a lobby");
            }
            
            var leftLobbyId = user.LobbyId;
            _userManager.LeaveLobby(connectionId);
            
            var usersInLobby = _userManager.GetUsersInLobby(leftLobbyId);
            
            await _hubContext.Clients.Group(leftLobbyId).SendAsync(ClientCallNames.UserLeftLobby, new UserLeftEvent
            {
                LeavingUser = user.Name,
                Usernames = usersInLobby.Select(u => u.Name).ToList()
            });
        }
    }
}