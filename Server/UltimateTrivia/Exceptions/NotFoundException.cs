using System;

namespace UltimateTrivia.Exceptions
{
    public class NotFoundException : ApplicationException
    {
        public NotFoundException(string message) : base(message)
        {
            
        }
    }
}