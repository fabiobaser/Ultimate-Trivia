using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BackgroundScheduler.JobData;
using BackgroundScheduler.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace BackgroundScheduler.HostedServices
{
    internal class InitialiseBackgroundScheduler : IHostedService
    {
        private readonly IScheduler _scheduler;
        private readonly IEnumerable<BackgroundJobReference> _jobReferences;
        private readonly ILogger<InitialiseBackgroundScheduler> _logger;
        private readonly IOptionsSnapshot<JobOptions> _jobOptions;

        public InitialiseBackgroundScheduler(IScheduler scheduler, IEnumerable<BackgroundJobReference> jobReferences, IOptions<JobOptions> jobOptions, ILogger<InitialiseBackgroundScheduler> logger)
        {
            _scheduler = scheduler;
            _jobReferences = jobReferences;
            _logger = logger;
            _jobOptions = jobOptions as IOptionsSnapshot<JobOptions>;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogDebug("starting background scheduler");
            await _scheduler.Start(cancellationToken);
            
            foreach (var jobReference in _jobReferences)
            {
                var jobName = jobReference.Type.FullName;
                var jobOption = _jobOptions.Get(jobName);
                    
                var jobDetail = JobBuilder.Create(jobReference.Type)
                    .WithIdentity(jobName)
                    .UsingStaticJobData(data =>
                    {
                        data.RetriesOnError = jobOption.RetriesOnError;
                        data.LogLevelOnError = jobOption.LogLevel ?? LogLevel.Error;
                        data.RetryAfterMin = jobOption.RetryAfterMin;
                    })
                    .Build();

                var trigger = TriggerBuilder.Create()
                    .WithIdentity($"{jobName}_trigger")
                    .StartNow()
                    .WithCronSchedule(jobOption.Cron)
                    .Build();

               await _scheduler.ScheduleJob(jobDetail, trigger, cancellationToken);
               
               _logger.LogInformation($"Background job {jobReference.Type.Name} registered! next execution date: {trigger.GetNextFireTimeUtc()}");
               
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogDebug("stopping background scheduler");
            await _scheduler.Shutdown(true, cancellationToken);
        }
    }
}