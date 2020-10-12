using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Trivia.Application;
using Trivia.Hubs.Events;
using Trivia.Services;

namespace Trivia.Hubs
{
    public class TriviaGameHub : Hub
    {
        private readonly UserManager _userManager;
        private readonly LobbyManager _lobbyManager;

        public TriviaGameHub(LobbyManager lobbyManager, UserManager userManager)
        {
            _userManager = userManager;
            _lobbyManager = lobbyManager;
        }
        
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            await LeaveLobby();
            _userManager.RemoveUser(Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }
        
        //TODO: Errorhandling and custom error event for all exceptions
        
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

            await Clients.Caller.SendAsync(ClientCallNames.JoinLobby, new JoinLobbyEvent
            {
                LobbyId = lobbyId,
                Usernames = userInLobby.Select(u => u.Name).ToList()
            });
            
        }
        
        public async Task CreateLobby(string username)
        {
            _userManager.AddUser(username, Context.ConnectionId);
            
            var lobby = await _lobbyManager.CreateLobbyAsync();
            await _lobbyManager.JoinLobbyAsync(lobby.Id, username, Context.ConnectionId);
            await Groups.AddToGroupAsync(Context.ConnectionId, lobby.Id);

            var userInLobby = _userManager.GetUsersInLobby(lobby.Id);

            await Clients.Caller.SendAsync(ClientCallNames.JoinLobby, new JoinLobbyEvent
            {
                LobbyId = lobby.Id,
                Usernames = userInLobby.Select(u => u.Name).ToList()
            });
        }

        public async Task LeaveLobby()
        {
            var user = _userManager.GetUserByConnectionId(Context.ConnectionId);
            if (user.LobbyId != null)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, user.LobbyId);
                await _lobbyManager.LeaveLobbyAsync(Context.ConnectionId);
                await Clients.Caller.SendAsync(ClientCallNames.LeaveLobby);
            }
        }

        public async Task CreateGame(int rounds, int duration)
        {
            // await _lobbyManager.
        }
    }
}