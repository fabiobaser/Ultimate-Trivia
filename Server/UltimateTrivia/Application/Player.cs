namespace UltimateTrivia.Application
{
    public class Player
    {
        public PlayerData Data { get; set; } = new PlayerData();
        public string ConnectionId { get; set; }
        
        public string IdentityId { get; set; }
        public string LobbyId { get; set; }
        
    }
    
    public class PlayerData
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string AvatarJson { get; set; }
    }
}