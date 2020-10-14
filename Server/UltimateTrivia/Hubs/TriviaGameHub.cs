using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using UltimateTrivia.Application;
using UltimateTrivia.Application.Game;
using UltimateTrivia.Constants;
using UltimateTrivia.Exceptions;
using UltimateTrivia.Hubs.Events;

namespace UltimateTrivia.Hubs
{
    public class TriviaGameHub : Hub
    {
        private readonly PlayerManager _playerManager;
        private readonly ILogger<TriviaGameHub> _logger;
        private readonly LobbyManager _lobbyManager;

        public TriviaGameHub(LobbyManager lobbyManager, PlayerManager playerManager, ILogger<TriviaGameHub> logger)
        {
            _playerManager = playerManager;
            _logger = logger;
            _lobbyManager = lobbyManager;
        }
        
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            _logger.LogInformation("Client {connectionId} disconnected", Context.ConnectionId);
            await LeaveLobby();
            _playerManager.RemovePlayer(Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }
        
        public async Task SendMessage(string message)
        {
            try
            {
                var player = _playerManager.GetPlayerByConnectionId(Context.ConnectionId);
            
                _logger.LogDebug("{username} posted message {message}", player.Name, message);
            
                await Clients.Group(player.LobbyId).SendAsync(RpcFunctionNames.BroadcastMessage, player.Name, message);
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
                _playerManager.AddPlayer(username, Context.ConnectionId);
            
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
                _playerManager.AddPlayer(username, Context.ConnectionId);
            
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
                var player = _playerManager.GetPlayerByConnectionId(Context.ConnectionId);
                if (player.LobbyId != null)
                {
                    _logger.LogDebug("{username} left lobby {lobbyId}", player.Name, player.LobbyId);
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, player.LobbyId);
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