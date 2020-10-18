using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using UltimateTrivia.Application;
using UltimateTrivia.Application.Game;
using UltimateTrivia.Constants;
using UltimateTrivia.Database.Game;
using UltimateTrivia.Exceptions;
using UltimateTrivia.Hubs.Events;

namespace UltimateTrivia.Hubs
{
    public class TriviaGameHub : Hub
    {
        private readonly PlayerManager _playerManager;
        private readonly UserRepository _userRepository;
        private readonly ILogger<TriviaGameHub> _logger;
        private readonly LobbyManager _lobbyManager;

        public TriviaGameHub(LobbyManager lobbyManager, PlayerManager playerManager, UserRepository userRepository, ILogger<TriviaGameHub> logger)
        {
            _playerManager = playerManager;
            _userRepository = userRepository;
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
            
                _logger.LogDebug("{userId} posted message {message}", player.Data.Id, message);
            
                await Clients.Group(player.LobbyId).SendAsync(RpcFunctionNames.BroadcastMessage, player.Data, message);
            }
            catch (Exception e)
            {
                await HandleException(e);
            }
        }
        
        public async Task JoinLobby(PlayerData playerData, string lobbyId)
        {
            try
            {
                if (Context.UserIdentifier != null)
                {
                    var user = await _userRepository.GetUserByIdentityId(Context.UserIdentifier);
                    user.Name = playerData.Name;
                    user.AvatarJson = playerData.AvatarJson;
                    await _userRepository.UpdateUser(user);
                    _playerManager.AddPlayer(playerData, Context.ConnectionId, user.Id);
                }
                else
                {
                    _playerManager.AddPlayer(playerData, Context.ConnectionId);
                }
                
                _logger.LogDebug("{playerId} joined lobby {lobbyId}", playerData.Id, lobbyId);
                
                await _lobbyManager.JoinLobbyAsync(lobbyId, playerData, Context.ConnectionId);
                await Groups.AddToGroupAsync(Context.ConnectionId, lobbyId);
            }
            catch (Exception e)
            {
                await HandleException(e);
            }
        }
        
        public async Task CreateLobby(PlayerData playerData)
        {
            try
            {
                if (Context.UserIdentifier != null)
                {
                    var user = await _userRepository.GetUserByIdentityId(Context.UserIdentifier);
                    user.Name = playerData.Name;
                    user.AvatarJson = playerData.AvatarJson;
                    await _userRepository.UpdateUser(user);
                    _playerManager.AddPlayer(playerData, Context.ConnectionId, user.Id);
                }
                else
                {
                    _playerManager.AddPlayer(playerData, Context.ConnectionId);
                }
            
                var lobby = await _lobbyManager.CreateLobbyAsync(playerData);
            
                _logger.LogDebug("{username} created lobby {lobbyId}", playerData.Id, lobby.Id);
            
                await _lobbyManager.JoinLobbyAsync(lobby.Id, playerData, Context.ConnectionId);
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
                    _logger.LogDebug("{username} left lobby {lobbyId}", player.Data.Id, player.LobbyId);
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, player.LobbyId);
                    await _lobbyManager.LeaveLobbyAsync(Context.ConnectionId);
                }
        
            }
            catch (Exception e)
            {
                await HandleException(e);
            }
        }

        public async Task StartGame(StartGameEvent startGameEvent)
        {
            try
            {
                await _lobbyManager.StartGameAsync(Context.ConnectionId, startGameEvent);
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
                await _lobbyManager.PassEventToGame(Context.ConnectionId, Game.EGameEvent.CategorySelected, category);
            }
            catch (Exception e)
            {
                await HandleException(e);
            }
        }
        
        public async Task AnswerSelected(string answerId)
        {
            try
            {
                await _lobbyManager.PassEventToGame(Context.ConnectionId, Game.EGameEvent.AnswerSelected,answerId);
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
                case DuplicatePlayerNameException duplicateUserNameException:
                    await Clients.Caller.SendAsync(RpcFunctionNames.Error, new ErrorEvent
                    {
                        ErrorCode = ErrorCodes.DuplicateUserName,
                        ErrorMessage = duplicateUserNameException.Message
                    });
                    break;
                case GameInProgressException gameInProgressException:
                    await Clients.Caller.SendAsync(RpcFunctionNames.Error, new ErrorEvent
                    {
                        ErrorCode = ErrorCodes.GameAlreadyInProgress,
                        ErrorMessage = gameInProgressException.Message
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