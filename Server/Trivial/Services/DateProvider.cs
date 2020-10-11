using System;

namespace Trivial.Services
{
    public class DateProvider : IDateProvider
    {
        public DateTimeOffset Now => DateTimeOffset.Now;
    }
}
