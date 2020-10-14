﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using UltimateTrivia.Application.Game;
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

        public IEnumerable<string> GetAllLobbyNames()
        {
            return Lobbies.Keys;
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
        
        public async Task<Lobby> CreateLobbyAsync(string playerName)
        {
            string lobbyId;
            do
            {
                lobbyId = _inviteCodeGenerator.GenerateCode();
            } 
            while (Lobbies.ContainsKey(lobbyId));
            
            var lobby = new Lobby(lobbyId, playerName);
            Lobbies[lobbyId] = lobby;

            return lobby;
        }
    
        public async Task JoinLobbyAsync(string lobbyId, string username, string connectionId)
        {
            if (!Lobbies.ContainsKey(lobbyId))
            {
                throw new ApplicationException("lobby doesnt exist");
            }

            var playerInLobby = _playerManager.GetPlayerInLobby(lobbyId).Select(u => u.Name).ToList();

            if (playerInLobby.Contains(username))
            {
                throw new DuplicateUserNameException("Username already taken");
            }
            
            _playerManager.JoinLobby(connectionId, lobbyId);

            playerInLobby = _playerManager.GetPlayerInLobby(lobbyId).Select(u => u.Name).ToList();

            await _hubContext.Clients.Group(lobbyId).SendAsync(RpcFunctionNames.UserJoinedLobby, new UserJoinedEvent
            {
                NewUser = username,
                Usernames = playerInLobby
            });
            
            await _hubContext.Clients.Client(connectionId).SendAsync(RpcFunctionNames.JoinLobby, new JoinLobbyEvent
            {
                LobbyId = lobbyId,
                Creator = Lobbies[lobbyId].Creator,
                Usernames = playerInLobby
            });
        }

        public async Task LeaveLobbyAsync(string connectionId)
        {
            var user = _playerManager.GetPlayerByConnectionId(connectionId);
            if (user.LobbyId == null)
            {
                throw new ApplicationException("user is not in a lobby");
            }
            
            var leftLobbyId = user.LobbyId;
            _playerManager.LeaveLobby(connectionId);
            
            var playerInLobby = _playerManager.GetPlayerInLobby(leftLobbyId);
            
            await _hubContext.Clients.Group(leftLobbyId).SendAsync(RpcFunctionNames.UserLeftLobby, new UserLeftEvent
            {
                LeavingUser = user.Name,
                Usernames = playerInLobby.Select(u => u.Name).ToList()
            });
        }

        public async Task StartGameAsync(string connectionId, StartGameEvent startGameEvent)
        {
            var player = _playerManager.GetPlayerByConnectionId(connectionId);

            var lobby = Lobbies[player.LobbyId];

            if (lobby == null)
            {
                throw new ApplicationException("User is not inside a lobby");
            }

            var gameId = _gameManager.CreateGame(configuration =>
            {
                configuration.LobbyId = player.LobbyId;
                configuration.Rounds = startGameEvent.Rounds;
                configuration.RoundDuration = startGameEvent.RoundDuration;
            });
            
            lobby.ConnectToGame(gameId);
            
            _gameManager.PassEventToGame(gameId, Application.Game.Game.GameStateTransition.StartGame);
        }

        public async Task PassEventToGame(string connectionId, Game.Game.GameStateTransition transition, string data)
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
                case Game.Game.GameStateTransition.CollectCategory:
                    _gameManager.PassEventToGame(lobby.GameId, transition, new CategorySelectedEvent
                    {
                        Category = data,
                        Username = player.Name
                    });
                    break;
                case Game.Game.GameStateTransition.CollectAnswers:
                    _gameManager.PassEventToGame(lobby.GameId, transition, new AnswerCollectedEvent()
                    {
                        Answer = data,
                        Username = player.Name
                    });
                    break;
            }
        }
    }
}