using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Trivia.Application
{
    public class UserManager
    {
        public ConcurrentDictionary<string, User> Users { get; set; }

        public UserManager()
        {
            Users = new ConcurrentDictionary<string, User>();
        }

        public List<User> GetAllUsers()
        {
            return Users.Select(u => u.Value).ToList();
        }
        
        public void AddUser(string username, string connectionId)
        {
            if (!Users.ContainsKey(connectionId))
            {
                Users[connectionId] = new User
                {
                    Name = username,
                    ConnectionId = connectionId
                };
            }
        }

        public void RemoveUser(string connectionId)
        {
            if (!Users.TryRemove(connectionId, out _))
            {
                throw new Exception($"failed to remove user with connectionId {connectionId}");
            }
        }

        public User GetUserByConnectionId(string connectionId)
        {
            return Users[connectionId];
        }

        public List<User> GetUsersInLobby(string lobbyId)
        {
            return Users.Where(u => u.Value.LobbyId == lobbyId).Select(dict => dict.Value).ToList();
        }

        public void JoinLobby(string connectionId, string lobbyId)
        {
            if (Users.ContainsKey(connectionId))
            {
                if (Users[connectionId].LobbyId != null)
                {
                    throw new Exception("User already joined a lobby");
                }

                Users[connectionId].LobbyId = lobbyId;
            }
            else
            {
                throw new ApplicationException($"User with connectionId {connectionId} doesnt exist");
            }
        }
        
        public void LeaveLobby(string connectionId)
        {
            if (Users.ContainsKey(connectionId))
            {
                Users[connectionId].LobbyId = null;
            }
            else
            {
                throw new ApplicationException($"User with connectionId {connectionId} doesnt exist");
            }
        }
    }    
}