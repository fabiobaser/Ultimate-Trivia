using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Trivia.Application
{
    public class UserManager
    {
        private readonly ConcurrentDictionary<string, User> _users;

        public UserManager()
        {
            _users = new ConcurrentDictionary<string, User>();
        }

        public List<User> GetAllUsers()
        {
            return _users.Select(u => u.Value).ToList();
        }

        public List<string> GetAllLobbyIds()
        {
            return _users.Where(kv => kv.Value.LobbyId != null).Select(kv => kv.Value.LobbyId).Distinct().ToList();
        }
        
        public void AddUser(string username, string connectionId)
        {
            if (!_users.ContainsKey(connectionId))
            {
                _users[connectionId] = new User
                {
                    Name = username,
                    ConnectionId = connectionId
                };
            }
        }

        public void RemoveUser(string connectionId)
        {
            if (!_users.TryRemove(connectionId, out _))
            {
                throw new Exception($"failed to remove user with connectionId {connectionId}");
            }
        }

        public User GetUserByConnectionId(string connectionId)
        {
            return _users[connectionId];
        }

        public List<User> GetUsersInLobby(string lobbyId)
        {
            return _users.Where(u => u.Value.LobbyId == lobbyId).Select(dict => dict.Value).ToList();
        }

        public void JoinLobby(string connectionId, string lobbyId)
        {
            if (_users.ContainsKey(connectionId))
            {
                if (_users[connectionId].LobbyId != null)
                {
                    throw new ApplicationException("User already joined a lobby");
                }

                _users[connectionId].LobbyId = lobbyId;
            }
            else
            {
                throw new ApplicationException($"User with connectionId {connectionId} doesnt exist");
            }
        }
        
        public void LeaveLobby(string connectionId)
        {
            if (_users.ContainsKey(connectionId))
            {
                _users[connectionId].LobbyId = null;
            }
            else
            {
                throw new ApplicationException($"User with connectionId {connectionId} doesnt exist");
            }
        }
    }    
}