using System;

namespace UltimateTrivia.Exceptions
{
    public class DuplicatePlayerNameException : ApplicationException
    {
        public DuplicatePlayerNameException(string message) : base(message)
        {
            
        }
    }
}