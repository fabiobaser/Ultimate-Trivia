using System;

namespace UltimateTrivia.Exceptions
{
    public class DuplicateUserNameException : ApplicationException
    {
        public DuplicateUserNameException(string message) : base(message)
        {
            
        }
    }
}