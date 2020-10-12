using System;
using Microsoft.Extensions.Logging;
using Quartz.Logging;

namespace BackgroundScheduler.Logger
{
    public class MicrosoftLogProvider : ILogProvider
    {
        private readonly ILoggerFactory _loggerFactory;

        public MicrosoftLogProvider(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
        }
        
        public Quartz.Logging.Logger GetLogger(string name)
        {
            var logger = _loggerFactory.CreateLogger(name);
            
            return new MicrosoftLoggerAdapter(logger).Log;
        }

        public IDisposable OpenNestedContext(string message)
        {
            throw new NotImplementedException();
        }

        public IDisposable OpenMappedContext(string key, string value)
        {
            throw new NotImplementedException();
        }
    }
}