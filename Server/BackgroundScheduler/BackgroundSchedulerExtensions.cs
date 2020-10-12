using BackgroundScheduler.HostedServices;
using BackgroundScheduler.Internal;
using BackgroundScheduler.Logger;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Quartz.Impl;
using Quartz.Logging;
using Quartz.Spi;

namespace BackgroundScheduler
{
    public static class BackgroundSchedulerExtensions
    {
        public static IBackgroundSchedulerBuilder AddBackgroundScheduler(this IServiceCollection services)
        {
            services.AddHostedService<InitialiseBackgroundScheduler>();
            services.TryAddSingleton<IJobFactory, JobFactory>();
            services.TryAddSingleton(provider =>
            {
                LogProvider.SetCurrentLogProvider(new MicrosoftLogProvider(provider.GetService<ILoggerFactory>()));

                var schedulerFactory = new StdSchedulerFactory();
                var scheduler = schedulerFactory.GetScheduler().GetAwaiter().GetResult();    
                scheduler.JobFactory = provider.GetService<IJobFactory>();
                return scheduler;
            });

            return new BackgroundSchedulerBuilder(services);
        }
    }
}