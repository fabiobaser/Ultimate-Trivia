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
        private readonly IHubContext<TrivialGameHub> _hubContext;
        private readonly ConcurrentBag<Lobby> _lobbies;

        public LobbyManager(IInviteCodeGenerator inviteCodeGenerator, IHubContext<TrivialGameHub> hubContext)
        {
            _inviteCodeGenerator = inviteCodeGenerator;
            _hubContext = hubContext;
            
            _lobbies = new ConcurrentBag<Lobby>();
        }

        public IEnumerable<string> GetAllLobbyNames()
        {
            return _lobbies.Select(l => l.Name);
        }
        
        public async Task<Lobby> CreateLobbyAsync()
        {
            string lobbyName;
            do
            {
                lobbyName = _inviteCodeGenerator.GenerateCode();
            } 
            while (_lobbies.Any(l => l.Name == lobbyName));
            
            var lobby = new Lobby(lobbyName);
            
            _lobbies.Add(lobby);
            return lobby;
        }
    
        public async Task JoinLobbyAsync(string lobbyName, string username, string connectionId)
        {
            var lobby = _lobbies.FirstOrDefault(l => l.Name == lobbyName);
            if (lobby == null)
            {
                throw new ApplicationException("lobby doesnt exist");
            }

            var user = new User
            {
                Name = username,
                ConnectionId = connectionId
            };

            await lobby.JoinAsync(user);

            await _hubContext.Clients.Group(lobbyName).SendAsync(ClientCallNames.BroadcastMessage,"Server", $"{username} joined the lobby");
        }

        public async Task LeaveLobbyAsync(string connectionId)
        {
            
        }
    }
}