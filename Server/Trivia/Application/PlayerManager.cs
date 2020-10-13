using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Trivia.Application
{
    public class PlayerManager
    {
        private readonly PlayerStore _playerStore;

        private ConcurrentDictionary<string, Player> Players => _playerStore.Players;
        
        public PlayerManager(PlayerStore playerStore)
        {
            _playerStore = playerStore;
        }

        public List<Player> GetAllPlayers()
        {
            return Players.Select(u => u.Value).ToList();
        }

        public List<string> GetAllLobbyIds()
        {
            return Players.Where(kv => kv.Value.LobbyId != null).Select(kv => kv.Value.LobbyId).Distinct().ToList();
        }
        
        public void AddPlayer(string playerName, string connectionId)
        {
            if (!Players.ContainsKey(connectionId))
            {
                Players[connectionId] = new Player
                {
                    Name = playerName,
                    ConnectionId = connectionId
                };
            }
        }

        public void RemovePlayer(string connectionId)
        {
            if (!Players.TryRemove(connectionId, out _))
            {
                throw new Exception($"failed to remove user with connectionId {connectionId}");
            }
        }

        public Player GetPlayerByConnectionId(string connectionId)
        {
            return Players[connectionId];
        }

        public List<Player> GetPlayerInLobby(string lobbyId)
        {
            return Players.Where(u => u.Value.LobbyId == lobbyId).Select(dict => dict.Value).ToList();
        }

        public void JoinLobby(string connectionId, string lobbyId)
        {
            if (Players.ContainsKey(connectionId))
            {
                if (Players[connectionId].LobbyId != null)
                {
                    throw new ApplicationException("User already joined a lobby");
                }

                Players[connectionId].LobbyId = lobbyId;
            }
            else
            {
                throw new ApplicationException($"User with connectionId {connectionId} doesnt exist");
            }
        }
        
        public void LeaveLobby(string connectionId)
        {
            if (Players.ContainsKey(connectionId))
            {
                Players[connectionId].LobbyId = null;
            }
            else
            {
                throw new ApplicationException($"User with connectionId {connectionId} doesnt exist");
            }
        }
    }    
}