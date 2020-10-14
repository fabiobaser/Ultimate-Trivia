using System;

namespace UltimateTrivia.Services
{
    public interface IDateProvider
    {
        DateTimeOffset Now { get; }
    }
}
