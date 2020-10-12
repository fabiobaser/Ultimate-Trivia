using System;
using Quartz;

namespace BackgroundScheduler.JobData
{
    internal static class JobDataExtensions
    {
        private const string RetriesCountKey = "retry_count";
        
        public static JobData GetStaticJobData(this JobDataMap source)
        {
            return new JobData(source);
        }

        public static void SetStaticJobData(this JobDataMap source, Action<JobData> jobDataAction)
        {
            var jobData = new JobData(source);
            jobDataAction(jobData);
        }

        public static int GetRetryCount(this JobDataMap source)
        {
            return source.ContainsKey(RetriesCountKey) ? source.GetIntValue(RetriesCountKey) : 0;
        }
        
        public static void IncrementRetryCount(this JobDataMap source)
        {
            if (!source.ContainsKey(RetriesCountKey))
            {
                source.Add(RetriesCountKey, 0);
            }

            var currentCount = (int) source[RetriesCountKey];
            source[RetriesCountKey] = ++currentCount;
        }
        
        public static JobBuilder UsingStaticJobData(this JobBuilder source, Action<JobData> jobDataAction)
        {
            var map = new JobDataMap();
            map.SetStaticJobData(jobDataAction);
            
            source.UsingJobData(map);
            
            return source;
        }
    }
}