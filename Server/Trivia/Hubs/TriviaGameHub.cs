using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Trivia.Application;
using Trivia.Application.Game;
using Trivia.Hubs.Events;
using Trivia.Services;

namespace Trivia.Hubs
{
    public class TriviaGameHub : Hub
    {
        private readonly UserManager _userManager;
        private readonly ILogger<TriviaGameHub> _logger;
        private readonly LobbyManager _lobbyManager;

        public TriviaGameHub(LobbyManager lobbyManager, UserManager userManager, ILogger<TriviaGameHub> logger)
        {
            _userManager = userManager;
            _logger = logger;
            _lobbyManager = lobbyManager;
        }
        
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            _logger.LogInformation("Client {connectionId} disconnected", Context.ConnectionId);
            await LeaveLobby();
            _userManager.RemoveUser(Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }
        
        //TODO: Errorhandling and custom error event for all exceptions
        
        public async Task SendMessage(string message)
        {
            var user = _userManager.GetUserByConnectionId(Context.ConnectionId);
            
            _logger.LogDebug("{username} posted message {message}", user.Name, message);
            
            await Clients.Group(user.LobbyId).SendAsync(ClientCallNames.BroadcastMessage, user.Name, message);
        }
        
        public async Task JoinLobby(string username, string lobbyId)
        {
            _logger.LogDebug("{username} joined lobby {lobbyId}", username, lobbyId);
            _userManager.AddUser(username, Context.ConnectionId);
            
            await _lobbyManager.JoinLobbyAsync(lobbyId, username, Context.ConnectionId);
            await Groups.AddToGroupAsync(Context.ConnectionId, lobbyId);
        }
        
        public async Task CreateLobby(string username)
        {
            _userManager.AddUser(username, Context.ConnectionId);
            
            var lobby = await _lobbyManager.CreateLobbyAsync(username);
            
            _logger.LogDebug("{username} created lobby {lobbyId}", username, lobby.Id);
            
            await _lobbyManager.JoinLobbyAsync(lobby.Id, username, Context.ConnectionId);
            await Groups.AddToGroupAsync(Context.ConnectionId, lobby.Id);
        }

        public async Task LeaveLobby()
        {
            var user = _userManager.GetUserByConnectionId(Context.ConnectionId);
            if (user.LobbyId != null)
            {
                _logger.LogDebug("{username} left lobby {lobbyId}", user.Name, user.LobbyId);
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, user.LobbyId);
                await _lobbyManager.LeaveLobbyAsync(Context.ConnectionId);
                await Clients.Caller.SendAsync(ClientCallNames.LeaveLobby);
            }
        }

        public async Task CreateGame(CreateGameEvent createGameEvent)
        {
            await _lobbyManager.CreateGameAsync(Context.ConnectionId, createGameEvent);
        }
        
        public async Task CategorySelected(string category)
        {
            await _lobbyManager.PassEventToGame(Context.ConnectionId, Game.GameStateTransition.CollectCategory, category);
        }
        
        public async Task AnswerSelected(string answer)
        {
            await _lobbyManager.PassEventToGame(Context.ConnectionId, Game.GameStateTransition.CollectAnswers,answer);
        }
    }
}