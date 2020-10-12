using System;
using System.Threading.Tasks;
using BackgroundScheduler.JobData;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Impl.Triggers;

namespace BackgroundScheduler.Internal
{
    public class JobWrapper: IJob
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Type _jobType;

        public JobWrapper(IServiceProvider serviceProvider, Type jobType)
        {
            _serviceProvider = serviceProvider;
            _jobType = jobType;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var job = scope.ServiceProvider.GetService(_jobType) as IJob;
                try
                {
                    await job.Execute(context);
                }
                catch (Exception e)
                {
                    RescheduleJobOnError(context, e, scope.ServiceProvider.GetService<ILogger<JobWrapper>>());
                }
            }
        }
        
        private void RescheduleJobOnError(IJobExecutionContext context, Exception ex, ILogger<JobWrapper> logger)
        {
            if (context.CancellationToken.IsCancellationRequested)
            {
                return;
            }
            
            var jobData = context.MergedJobDataMap.GetStaticJobData();

            if (jobData.RetriesOnError == 0)
            {
                logger.Log(jobData.LogLevelOnError, ex, $"Job '{context.JobDetail.JobType.Name}' failed without retries");
                return;
            }

            var retryCount = context.MergedJobDataMap.GetRetryCount();
            if (retryCount >= jobData.RetriesOnError)
            {
                logger.Log(jobData.LogLevelOnError, ex, $"Job '{context.JobDetail.JobType.Name}' failed with {retryCount} retries");
                return;
            }

            context.MergedJobDataMap.IncrementRetryCount();

            var retryTrigger = new SimpleTriggerImpl(Guid.NewGuid().ToString())
            {
                Description = "RetryTrigger",
                RepeatCount = 0,
                JobKey = context.JobDetail.Key,
                JobDataMap = context.MergedJobDataMap,
                StartTimeUtc = DateBuilder.FutureDate(jobData.RetryAfterMin, IntervalUnit.Minute)
            };

            context.Scheduler.ScheduleJob(retryTrigger);
            logger.LogDebug(ex, "Scheduled {0}, next execution time is {1}. {2}/{3} retries", context.JobDetail.JobType.Name, retryTrigger.GetFireTimeAfter(DateTime.Now), retryCount+1, jobData.RetriesOnError);
        }
    }
}