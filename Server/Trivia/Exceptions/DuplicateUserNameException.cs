using System;

namespace Trivia.Exceptions
{
    public class DuplicateUserNameException : ApplicationException
    {
        public DuplicateUserNameException(string message) : base(message)
        {
            
        }
    }
}