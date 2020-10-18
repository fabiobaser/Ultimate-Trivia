namespace UltimateTrivia.Database.Game.Entities
{
    public class User : AuditableEntity
    {
        public string Id { get; set; }
        public string IdentityId { get; set; }
        public string Name { get; set; }
        public string AvatarJson { get; set; }
    }
}