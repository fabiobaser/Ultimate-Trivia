using System;
using Quartz;
using Quartz.Spi;

namespace BackgroundScheduler.Internal
{
    internal class JobFactory : IJobFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public JobFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
        {
            var jobWrapper = new JobWrapper(_serviceProvider, bundle.JobDetail.JobType);
            return jobWrapper;
        }

        public void ReturnJob(IJob job)
        {
        }
    }
}