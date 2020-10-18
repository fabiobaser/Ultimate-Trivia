using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using UltimateTrivia.Application.Game.TransitionData;
using UltimateTrivia.Constants;
using UltimateTrivia.Exceptions;
using UltimateTrivia.Hubs;
using UltimateTrivia.Hubs.Events;
using UltimateTrivia.Services;

namespace UltimateTrivia.Application
{
    public class LobbyManager
    {
        private readonly LobbyStore _lobbyStore;
        private readonly IInviteCodeGenerator _inviteCodeGenerator;
        private readonly PlayerManager _playerManager;
        private readonly IHubContext<TriviaGameHub> _hubContext;
        private readonly GameManager _gameManager;
        private readonly ILogger<LobbyManager> _logger;

        private ConcurrentDictionary<string, Lobby> Lobbies => _lobbyStore.Lobbies;
        
        public LobbyManager(LobbyStore lobbyStore, IInviteCodeGenerator inviteCodeGenerator, PlayerManager playerManager, IHubContext<TriviaGameHub> hubContext, GameManager gameManager, ILogger<LobbyManager> logger)
        {
            _lobbyStore = lobbyStore;
            _inviteCodeGenerator = inviteCodeGenerator;
            _playerManager = playerManager;
            _hubContext = hubContext;
            _gameManager = gameManager;
            _logger = logger;
        }

        public List<Lobby> GetAllLobbies()
        {
            return Lobbies.Select(l => l.Value).ToList();
        }
        
        public Lobby GetLobbyById(string lobbyId)
        {
            if (Lobbies.TryGetValue(lobbyId, out var lobby))
            {
                return lobby;
            }
            throw new NotFoundException($"lobby {lobbyId} doesnt exist");
        }

        public void DeleteLobby(string lobbyId)
        {
            _logger.LogDebug("deleting lobby {lobbyId}", lobbyId);
            if (!Lobbies.TryRemove(lobbyId, out var lobby))
            {
                throw new ApplicationException($"lobby {lobbyId} couldnt be deleted");
            }

            if (lobby.GameId != null)
            {
                _gameManager.DeleteGame(lobby.GameId);
            }
        }
        
        public async Task<Lobby> CreateLobbyAsync(PlayerData playerData)
        {
            string lobbyId;
            do
            {
                lobbyId = _inviteCodeGenerator.GenerateCode();
            } 
            while (Lobbies.ContainsKey(lobbyId));
            
            var lobby = new Lobby(lobbyId, playerData.Id);
            Lobbies[lobbyId] = lobby;

            return lobby;
        }
    
        public async Task JoinLobbyAsync(string lobbyId, PlayerData playerData, string connectionId)
        {
            if (!Lobbies.ContainsKey(lobbyId))
            {
                throw new ApplicationException("lobby doesnt exist");
            }

            var playerInLobby = _playerManager.GetPlayerInLobby(lobbyId).Select(u => u.Data).ToList();

            if (playerInLobby.Any(p => p.Name == playerData.Name))
            {
                throw new DuplicatePlayerNameException("Username already taken");
            }
            
            if (playerInLobby.Any(p => p.Id == playerData.Id))
            {
                throw new ApplicationException("User already joined this lobby");
            }
            
            _playerManager.JoinLobby(connectionId, lobbyId);

            playerInLobby = _playerManager.GetPlayerInLobby(lobbyId).Select(u => u.Data).ToList();

            await _hubContext.Clients.Group(lobbyId).SendAsync(RpcFunctionNames.UserJoinedLobby, new PlayerJoinedEvent
            {
                NewPlayer = playerData,
                Players = playerInLobby
            });
            
            await _hubContext.Clients.Client(connectionId).SendAsync(RpcFunctionNames.JoinLobby, new JoinLobbyEvent
            {
                LobbyId = lobbyId,
                CreatorId = Lobbies[lobbyId].CreatorId,
                Player = playerData,
                Players = playerInLobby
            });
            
            var lobby = Lobbies[lobbyId];
            if (lobby.GameId != null && _gameManager.IsGameInProgress(lobby.GameId))
            {
                _gameManager.PassEventToGame(lobby.GameId, Game.Game.EGameEvent.PlayerJoined, new PlayerJoinedData
                {
                    ConnectionId = connectionId,
                    Player = playerData
                });
            }
        }

        public async Task LeaveLobbyAsync(string connectionId)
        {
            var player = _playerManager.GetPlayerByConnectionId(connectionId);
            if (player.LobbyId == null)
            {
                throw new ApplicationException("user is not in a lobby");
            }
            
            var leftLobbyId = player.LobbyId;
            _playerManager.LeaveLobby(connectionId);
            
            var lobby = Lobbies[leftLobbyId];
            if (lobby.GameId != null && _gameManager.IsGameInProgress(lobby.GameId))
            {
                _gameManager.PassEventToGame(lobby.GameId, Game.Game.EGameEvent.PlayerLeft, new PlayerLeftData
                {
                    LeavingPlayer = player.Data
                });
            }
            
            var playerInLobby = _playerManager.GetPlayerInLobby(leftLobbyId);
            
            await _hubContext.Clients.Group(leftLobbyId).SendAsync(RpcFunctionNames.UserLeftLobby, new PlayerLeftEvent
            {
                LeavingPlayer = player.Data,
                Players = playerInLobby.Select(u => u.Data).ToList()
            });
            
            await _hubContext.Clients.Client(connectionId).SendAsync(RpcFunctionNames.LeaveLobby);
        }

        public async Task StartGameAsync(string connectionId, StartGameEvent startGameEvent)
        {
            var player = _playerManager.GetPlayerByConnectionId(connectionId);

            var lobby = Lobbies[player.LobbyId];

            if (lobby == null)
            {
                throw new ApplicationException("User is not in a lobby");
            }

            if (lobby.GameId == null)
            {
                var gameId = _gameManager.CreateGame(configuration =>
                {
                    configuration.LobbyId = player.LobbyId;
                });
            
                lobby.ConnectToGame(gameId);
                _gameManager.PassTransitionToGame(gameId, Game.Game.EGameCommand.StartGame, new GameStartedData
                {
                    Rounds = startGameEvent.Rounds,
                    AnswerDuration = startGameEvent.AnswerDuration
                });
            }
            else
            {
                if (_gameManager.IsGameInProgress(lobby.GameId))
                {
                    throw new GameInProgressException($"Game {lobby.GameId} is already in progress");
                }

                _gameManager.PassTransitionToGame(lobby.GameId, Game.Game.EGameCommand.StartGame,
                    new GameStartedData
                    {
                        Rounds = startGameEvent.Rounds,
                        AnswerDuration = startGameEvent.AnswerDuration
                    });
            }
        }

        public async Task PassEventToGame(string connectionId, Game.Game.EGameEvent transition, string data)
        {
            var player = _playerManager.GetPlayerByConnectionId(connectionId);

            var lobby = Lobbies[player.LobbyId];

            if (lobby == null)
            {
                throw new ApplicationException("User is not inside a lobby");
            }

            if (lobby.GameId == null)
            {
                throw new ApplicationException($"no running Game in lobby {lobby.Id}");
            }

            switch (transition)
            {
                case Game.Game.EGameEvent.CategorySelected:
                    _gameManager.PassEventToGame(lobby.GameId, transition, new CategorySelectedData
                    {
                        Category = data,
                        Player = player.Data
                    });
                    break;
                case Game.Game.EGameEvent.AnswerSelected:
                    _gameManager.PassEventToGame(lobby.GameId, transition, new AnswerSelectedData
                    {
                        AnswerId = data,
                        Player = player.Data
                    });
                    break;
            }
        }
    }
}