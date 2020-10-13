using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Trivia.Application;
using Trivia.Application.Game;
using Trivia.Constants;
using Trivia.Exceptions;
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
        
        public async Task SendMessage(string message)
        {
            try
            {
                var user = _userManager.GetUserByConnectionId(Context.ConnectionId);
            
                _logger.LogDebug("{username} posted message {message}", user.Name, message);
            
                await Clients.Group(user.LobbyId).SendAsync(RpcFunctionNames.BroadcastMessage, user.Name, message);
            }
            catch (Exception e)
            {
                await HandleException(e);
            }
        }
        
        public async Task JoinLobby(string username, string lobbyId)
        {
            try
            {
                _logger.LogDebug("{username} joined lobby {lobbyId}", username, lobbyId);
                _userManager.AddUser(username, Context.ConnectionId);
            
                await _lobbyManager.JoinLobbyAsync(lobbyId, username, Context.ConnectionId);
                await Groups.AddToGroupAsync(Context.ConnectionId, lobbyId);
            }
            catch (Exception e)
            {
                await HandleException(e);
            }
        }
        
        public async Task CreateLobby(string username)
        {
            try
            {
                _userManager.AddUser(username, Context.ConnectionId);
            
                var lobby = await _lobbyManager.CreateLobbyAsync(username);
            
                _logger.LogDebug("{username} created lobby {lobbyId}", username, lobby.Id);
            
                await _lobbyManager.JoinLobbyAsync(lobby.Id, username, Context.ConnectionId);
                await Groups.AddToGroupAsync(Context.ConnectionId, lobby.Id);
        
            }
            catch (Exception e)
            {
                await HandleException(e);
            }
        }

        public async Task LeaveLobby()
        {
            try
            {
                var user = _userManager.GetUserByConnectionId(Context.ConnectionId);
                if (user.LobbyId != null)
                {
                    _logger.LogDebug("{username} left lobby {lobbyId}", user.Name, user.LobbyId);
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, user.LobbyId);
                    await _lobbyManager.LeaveLobbyAsync(Context.ConnectionId);
                    await Clients.Caller.SendAsync(RpcFunctionNames.LeaveLobby);
                }
        
            }
            catch (Exception e)
            {
                await HandleException(e);
            }
        }

        public async Task CreateGame(CreateGameEvent createGameEvent)
        {
            try
            {
                await _lobbyManager.CreateGameAsync(Context.ConnectionId, createGameEvent);
            }
            catch (Exception e)
            {
                await HandleException(e);
            }
        }
        
        public async Task CategorySelected(string category)
        {
            try
            {
                await _lobbyManager.PassEventToGame(Context.ConnectionId, Game.GameStateTransition.CollectCategory, category);
            }
            catch (Exception e)
            {
                await HandleException(e);
            }
        }
        
        public async Task AnswerSelected(string answer)
        {
            try
            {
                await _lobbyManager.PassEventToGame(Context.ConnectionId, Game.GameStateTransition.CollectAnswers,answer);
            }
            catch (Exception e)
            {
                await HandleException(e);
            }
        }

        private async Task HandleException(Exception exception)
        {
            _logger.LogError(exception, "exception during message");
            
            switch (exception)
            {
                case DuplicateUserNameException duplicateUserNameException:
                    await Clients.Caller.SendAsync(RpcFunctionNames.Error, new ErrorEvent
                    {
                        ErrorCode = ErrorCodes.DuplicateUserName,
                        ErrorMessage = duplicateUserNameException.Message
                    });
                    break;
                default:
                    await Clients.Caller.SendAsync(RpcFunctionNames.Error, new ErrorEvent
                    {
                        ErrorCode = ErrorCodes.InternalServerError,
                        ErrorMessage = exception.Message
                    });
                    break;
            }
        }
    }
}