namespace UltimateTrivia.Application
{
    public class Lobby
    {
        public string Id { get; set; }
        public string CreatorId { get; set; }
        public string GameId { get; set; }
        
        public Lobby(string lobbyId, string creatorIdId)
        {
            CreatorId = creatorIdId;
            Id = lobbyId;
        }

        public void ConnectToGame(string gameId)
        {
            GameId = gameId;
        }
    }
}