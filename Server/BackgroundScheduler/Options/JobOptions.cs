using Microsoft.Extensions.Logging;

namespace BackgroundScheduler.Options
{
    public class JobOptions
    {
        public string Cron { get; set; }
        public int RetriesOnError { get; set; }
        public LogLevel? LogLevel { get; set; }
        public int RetryAfterMin { get; set; }
    }
}