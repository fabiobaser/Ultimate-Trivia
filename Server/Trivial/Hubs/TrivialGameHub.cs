using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Trivial.Application;

namespace Trivial.Hubs
{
    public class TrivialGameHub : Hub
    {
        private readonly LobbyManager _lobbyManager;

        public TrivialGameHub(LobbyManager lobbyManager)
        {
            _lobbyManager = lobbyManager;
        }
        
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            await _lobbyManager.LeaveLobbyAsync(Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }
        
        
        public async Task Send(string name, string message)
        {
            await Clients.All.SendAsync(ClientCallNames.BroadcastMessage, name, message);
            await Clients.Group("group1").SendAsync(ClientCallNames.BroadcastMessage, name, message);
        }
        
        public async Task JoinLobby(string username, string lobbyName)
        {
            await _lobbyManager.JoinLobbyAsync(lobbyName, username, Context.ConnectionId);
            await Groups.AddToGroupAsync(Context.ConnectionId, lobbyName);
        }
        
        public async Task CreateLobby(string username)
        {
            var lobby = await _lobbyManager.CreateLobbyAsync();
            await _lobbyManager.JoinLobbyAsync(lobby.Name, username, Context.ConnectionId);
            await Groups.AddToGroupAsync(Context.ConnectionId, lobby.Name);
            await Clients.Caller.SendAsync(ClientCallNames.BroadcastMessage, "Server", $"lobby created. Invite your friends {lobby.Name}");
        }

        public async Task CreateGame(int rounds, int duration)
        {
            // await _lobbyManager.
        }
    }
}