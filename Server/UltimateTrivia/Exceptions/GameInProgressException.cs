using System;

namespace UltimateTrivia.Exceptions
{
    public class GameInProgressException : ApplicationException
    {
        public GameInProgressException(string message) : base(message)
        {
            
        }
    }
}