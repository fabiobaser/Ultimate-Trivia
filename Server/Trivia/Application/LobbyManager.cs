using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Trivia.Hubs;
using Trivia.Services;

namespace Trivia.Application
{
    public class LobbyManager
    {
        private readonly IInviteCodeGenerator _inviteCodeGenerator;
        private readonly UserManager _userManager;
        private readonly IHubContext<TrivialGameHub> _hubContext;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly ConcurrentDictionary<string, Lobby> _lobbies;

        public LobbyManager(IInviteCodeGenerator inviteCodeGenerator, UserManager userManager, IHubContext<TrivialGameHub> hubContext, IJsonSerializer jsonSerializer)
        {
            _inviteCodeGenerator = inviteCodeGenerator;
            _userManager = userManager;
            _hubContext = hubContext;
            _jsonSerializer = jsonSerializer;

            _lobbies = new ConcurrentDictionary<string, Lobby>();
        }

        public IEnumerable<string> GetAllLobbyNames()
        {
            return _lobbies.Keys;
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
            var serializedUsers = _jsonSerializer.Serialize(usersInLobby.Select(u => u.Name));
            
            await _hubContext.Clients.Group(lobbyId).SendAsync(ClientCallNames.UserJoinedLobby, username, serializedUsers);
        }

        public async Task LeaveLobbyAsync(string connectionId)
        {
            var user = _userManager.GetUserByConnectionId(connectionId);
            var leftLobbyId = user.LobbyId;
            _userManager.LeaveLobby(connectionId);
            
            var usersInLobby = _userManager.GetUsersInLobby(leftLobbyId);
            var serializedUsers = _jsonSerializer.Serialize(usersInLobby.Select(u => u.Name));
            
            await _hubContext.Clients.Group(leftLobbyId).SendAsync(ClientCallNames.UserLeftLobby, user.Name, serializedUsers);
        }
    }
}