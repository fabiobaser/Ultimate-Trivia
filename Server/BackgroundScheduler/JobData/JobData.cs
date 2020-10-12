using System;
using Microsoft.Extensions.Logging;
using Quartz;

namespace BackgroundScheduler.JobData
{
    internal class JobData
    {
        private const string RetriesOnErrorKey = "retries_on_error";
        private const string LogLevelOnErrorKey = "loglevel_on_error";
        private const string RetryAfterMinKey = "retry_after_min";
        
        private readonly JobDataMap _map;

        public JobData(JobDataMap map)
        {
            _map = map;
        }

        public int RetriesOnError
        {
            get
            {
                if (_map.ContainsKey(RetriesOnErrorKey))
                {
                    return _map.GetIntValue(RetriesOnErrorKey);
                }

                return 0;
            }
            set
            {
                if (!_map.ContainsKey(RetriesOnErrorKey))
                {
                    _map.Add(RetriesOnErrorKey, value);
                }
                else
                {
                    _map[RetriesOnErrorKey] = value;
                }
            }
        }

        public LogLevel LogLevelOnError
        {
            get
            {
                if (_map.ContainsKey(LogLevelOnErrorKey))
                {
                    return (LogLevel) Enum.Parse(typeof(LogLevel), _map.GetString(LogLevelOnErrorKey));
                }

                return LogLevel.Error;
            }
            set
            {
                if (!_map.ContainsKey(LogLevelOnErrorKey))
                {
                    _map.Add(LogLevelOnErrorKey, value.ToString("G"));
                }
                else
                {
                    _map[LogLevelOnErrorKey] = value.ToString("G");
                }
            }
        }

        public int RetryAfterMin
        {
            get
            {
                if (_map.ContainsKey(RetryAfterMinKey))
                {
                    return _map.GetIntValue(RetryAfterMinKey);
                }

                return 0;
            }
            set
            {
                if (!_map.ContainsKey(RetryAfterMinKey))
                {
                    _map.Add(RetryAfterMinKey, value);
                }
                else
                {
                    _map[RetryAfterMinKey] = value;
                }
            }
        }
    }
}