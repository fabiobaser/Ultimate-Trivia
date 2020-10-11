using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Trivia.Application;
using Trivia.Services;

namespace Trivia.Hubs
{
    public class TrivialGameHub : Hub
    {
        private readonly UserManager _userManager;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly LobbyManager _lobbyManager;

        public TrivialGameHub(LobbyManager lobbyManager, UserManager userManager, IJsonSerializer jsonSerializer)
        {
            _userManager = userManager;
            _jsonSerializer = jsonSerializer;
            _lobbyManager = lobbyManager;
        }
        
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            await LeaveLobby();
            _userManager.RemoveUser(Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }
        
        public async Task SendMessage(string message)
        {
            var user = _userManager.GetUserByConnectionId(Context.ConnectionId);
            
            await Clients.Group(user.LobbyId).SendAsync(ClientCallNames.BroadcastMessage, user.Name, message);
        }
        
        public async Task JoinLobby(string username, string lobbyId)
        {
            _userManager.AddUser(username, Context.ConnectionId);
            
            await _lobbyManager.JoinLobbyAsync(lobbyId, username, Context.ConnectionId);
            await Groups.AddToGroupAsync(Context.ConnectionId, lobbyId);
            
            var userInLobby = _userManager.GetUsersInLobby(lobbyId);

            var serializedUsers = _jsonSerializer.Serialize(userInLobby.Select(u => u.Name));
            
            await Clients.Caller.SendAsync(ClientCallNames.JoinLobby, lobbyId, serializedUsers);
            
        }
        
        public async Task CreateLobby(string username)
        {
            _userManager.AddUser(username, Context.ConnectionId);
            
            var lobby = await _lobbyManager.CreateLobbyAsync();
            await _lobbyManager.JoinLobbyAsync(lobby.Id, username, Context.ConnectionId);
            await Groups.AddToGroupAsync(Context.ConnectionId, lobby.Id);

            var userInLobby = _userManager.GetUsersInLobby(lobby.Id);
            var serializedUsers = _jsonSerializer.Serialize(userInLobby.Select(u => u.Name));
            
            await Clients.Caller.SendAsync(ClientCallNames.JoinLobby, lobby.Id, serializedUsers);
        }

        public async Task LeaveLobby()
        {
            var user = _userManager.GetUserByConnectionId(Context.ConnectionId);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, user.LobbyId);
            await _lobbyManager.LeaveLobbyAsync(Context.ConnectionId);
            await Clients.Caller.SendAsync(ClientCallNames.LeaveLobby);
        }

        public async Task CreateGame(int rounds, int duration)
        {
            // await _lobbyManager.
        }
    }
}