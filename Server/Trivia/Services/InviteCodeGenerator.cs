using System;
using System.Text;

namespace Trivia.Services
{
    public class InviteCodeGenerator : IInviteCodeGenerator
    {
        private Random _random = new Random();
        private const int Length = 6;
        
        
        public string GenerateCode()
        {
            var sb = new StringBuilder();

            var offset = 'A';
            const int lettersOffset = 26;
  
            for (var i = 0; i < Length; i++)  
            {  
                var c = (char)_random.Next(offset, offset + lettersOffset);  
                sb.Append(c);
            }

            return sb.ToString();
        }
    }
}