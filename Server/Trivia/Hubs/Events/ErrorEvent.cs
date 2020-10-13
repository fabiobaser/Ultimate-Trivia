namespace Trivia.Hubs.Events
{
    public class ErrorEvent
    {
        public string ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
    }
}