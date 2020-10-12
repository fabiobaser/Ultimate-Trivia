using System.Collections.Generic;

namespace Trivia.Database.Entities
{
    public class Question
    {
        public string Id { get; set; }
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