using System;
using BackgroundScheduler.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Quartz;

namespace BackgroundScheduler.Internal
{
    internal class BackgroundSchedulerBuilder : IBackgroundSchedulerBuilder
    {
        private readonly IServiceCollection _serviceCollection;
        
        public BackgroundSchedulerBuilder(IServiceCollection serviceCollection)
        {
            _serviceCollection = serviceCollection;
        }

        public IBackgroundSchedulerBuilder AddJob<T>(Action<JobOptions> optionsAction)
            where T : class, IJob
        {
            _serviceCollection.Configure(typeof(T).FullName, optionsAction);
            return AddJob<T>();
        }

        public IBackgroundSchedulerBuilder AddJob<T>(IConfiguration configuration)
            where T : class, IJob
        {
            _serviceCollection.Configure<JobOptions>(typeof(T).FullName, configuration.GetSection("BackgroundJobs").GetSection(typeof(T).Name));
            return AddJob<T>();
        }

        public IBackgroundSchedulerBuilder AddOptionsProvider<TOptionsProvider>()
            where TOptionsProvider : class, IConfigureNamedOptions<JobOptions>
        {
            _serviceCollection.AddTransient<IConfigureOptions<JobOptions>, TOptionsProvider>();

            return this;
        }
        
        private IBackgroundSchedulerBuilder AddJob<T>()
            where T : class, IJob
        {
            _serviceCollection.TryAddSingleton(BackgroundJobReference.Create<T>());
            _serviceCollection.TryAddTransient<T>();
            return this;
        }
    }
}