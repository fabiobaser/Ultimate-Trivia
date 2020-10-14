using System;

namespace UltimateTrivia.Database.Game.Entities
{
    public class Answer : AuditableEntity
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Content { get; set; }
        public bool IsCorrectAnswer { get; set; }
        
        public string QuestionId { get; set; }
        public virtual Question Question { get; set; }
    }
}