  using Trivia.Identity;

  namespace Trivia.Services
{
    public interface ICurrentUserService
    {
        User GetCurrentUser();
    }
}
