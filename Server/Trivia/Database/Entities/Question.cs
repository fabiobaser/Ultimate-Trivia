using System;
using System.Collections.Generic;

namespace Trivia.Database.Entities
{
    public class Question : AuditableEntity
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Content { get; set; }
        public string Category { get; set; }
        public QuestionType Type { get; set; }
        
        public virtual ICollection<Answer> Answers { get; set; }
        
        public enum QuestionType
        {
            None = 0,
            MultipleChoice = 1
        }
    }
}