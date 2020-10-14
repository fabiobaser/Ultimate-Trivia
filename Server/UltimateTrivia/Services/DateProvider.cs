using System;

namespace UltimateTrivia.Services
{
    public class DateProvider : IDateProvider
    {
        public DateTimeOffset Now => DateTimeOffset.Now;
    }
}
