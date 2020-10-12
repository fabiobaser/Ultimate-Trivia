using System;
using Microsoft.Extensions.Logging;
using LogLevel = Quartz.Logging.LogLevel;

namespace BackgroundScheduler.Logger
{
    public class MicrosoftLoggerAdapter
    {
        private readonly ILogger _logger;

        public MicrosoftLoggerAdapter(ILogger logger)
        {
            _logger = logger;
        }
        
        public bool Log(LogLevel loglevel, Func<string> messagefunc, Exception exception, object[] formatparameters)
        {
            if (messagefunc == null)
            {
                return _logger.IsEnabled((Microsoft.Extensions.Logging.LogLevel) loglevel);
            }
            
            switch (loglevel)
            {
                case LogLevel.Trace:
                    _logger.LogTrace(exception, messagefunc(), formatparameters);
                    return true;
                case LogLevel.Debug:
                    _logger.LogDebug(exception, messagefunc(), formatparameters);
                    return true;
                case LogLevel.Info:
                    _logger.LogInformation(exception, messagefunc(), formatparameters);
                    return true;
                case LogLevel.Warn:
                    _logger.LogWarning(exception, messagefunc(), formatparameters);
                    return true;
                case LogLevel.Error:
                    _logger.LogError(exception, messagefunc(), formatparameters);
                    return true;
                case LogLevel.Fatal:
                    _logger.LogCritical(exception, messagefunc(), formatparameters);
                    return true;
                default:
                    throw new ArgumentOutOfRangeException(nameof(loglevel), loglevel, null);
            }
        }
    }
}