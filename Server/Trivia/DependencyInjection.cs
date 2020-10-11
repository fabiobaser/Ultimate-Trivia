using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Trivia.OpenApi;
using Trivia.Services;

namespace Trivia
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddUtils(this IServiceCollection services)
        {
            return services.AddDateProvider()
                .AddInviteCodeGenerator();
        }
        
        public static IServiceCollection AddDateProvider(this IServiceCollection services)
        {
            services.AddTransient<IDateProvider, DateProvider>();
            return services;
        }
        
        public static IServiceCollection AddInviteCodeGenerator(this IServiceCollection services)
        {
            services.AddTransient<IInviteCodeGenerator, InviteCodeGenerator>();
            return services;
        }
        
        public static IMvcBuilder AddJsonSerializer(this IMvcBuilder builder)
        {
            builder.Services.TryAddSingleton<IJsonSerializer>(provider =>
            {
                var config = provider.GetRequiredService<IOptions<MvcNewtonsoftJsonOptions>>();
                
                return new JsonSerializer(config.Value.SerializerSettings);
            });
            
            return builder;
        }

        public static IServiceCollection AddApiVersioningAndExplorer(this IServiceCollection services)
        {
            services.AddApiVersioning(
                options =>
                {
                    options.ReportApiVersions = true;
                    options.DefaultApiVersion = new ApiVersion(1,0);
                    options.AssumeDefaultVersionWhenUnspecified = true;
                }
            );

            services.AddVersionedApiExplorer(
                options =>
                {
                    options.GroupNameFormat = "'v'VVV";
                    options.SubstituteApiVersionInUrl = true;
                });
            
            return services;
        }
        
        public static IServiceCollection AddOpenApi(this IServiceCollection services)
        {
            services.ConfigureOptions<ConfigureSwaggerOptions>();
            services.AddSwaggerGen(
                options =>
                {
                    options.OperationFilter<SwaggerDefaultValues>();
                });

            return services;
        }

        public static IApplicationBuilder UseOpenApi(this IApplicationBuilder app)
        {
            var provider = app.ApplicationServices.GetRequiredService<IApiVersionDescriptionProvider>();
            app.UseSwagger();

            app.UseSwaggerUI(options =>
            {
                foreach(var description in provider.ApiVersionDescriptions)
                {
                    options.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json", description.GroupName.ToUpperInvariant());
                }
            });

            return app;
        }
    }
}