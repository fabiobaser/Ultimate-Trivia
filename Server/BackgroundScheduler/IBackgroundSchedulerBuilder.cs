using System;
using BackgroundScheduler.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Quartz;

namespace BackgroundScheduler
{
    public interface IBackgroundSchedulerBuilder
    {
        IBackgroundSchedulerBuilder AddJob<T>(Action<JobOptions> optionsAction)
            where T : class, IJob;
        
        IBackgroundSchedulerBuilder AddJob<T>(IConfiguration configuration)
            where T : class, IJob;

        IBackgroundSchedulerBuilder AddOptionsProvider<TOptionsProvider>()
            where TOptionsProvider : class, IConfigureNamedOptions<JobOptions>;
    }
}