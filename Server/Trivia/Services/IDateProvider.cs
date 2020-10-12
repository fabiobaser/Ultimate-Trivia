using System;

  namespace Trivia.Services
{
    public interface IDateProvider
    {
        DateTimeOffset Now { get; }
    }
}
