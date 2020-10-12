using System.Text.Json;
using BackgroundScheduler;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Trivia.Application;
using Trivia.Application.Game;
using Trivia.BackgroundJobs;
using Trivia.Database;
using Trivia.HostedServices;
using Trivia.Hubs;
using Trivia.Middlewares;

namespace Trivia
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private readonly IConfiguration _configuration;

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<LobbyManager>();
            services.AddTransient<UserManager>();

            services.AddSingleton<UserStore>();
            services.AddSingleton<LobbyStore>();

            services.AddTransient<QuestionRepository>();

            services.AddSingleton<GameManager>();
            
            services.AddUtils();
            services.AddHttpContextAccessor();

            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseNpgsql(_configuration.GetConnectionString("DefaultConnection"));
            });
            
            services.AddHealthChecks()
                .AddDbContextCheck<ApplicationDbContext>();
            
            services.AddControllers()
                .AddNewtonsoftJson(options => // TODO: change custom serializer to system.text.json
                {
                    options.SerializerSettings.Converters.Add(new StringEnumConverter());
                    options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                    options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                }).AddJsonSerializer();

            services.AddCors(options =>
                options
                    .AddDefaultPolicy(builder =>
                        builder
                            .WithOrigins("http://localhost:1234", "http://localhost:5000", "http://marceljenner.com:5000", "http://marceljenner.com:1234", "http://marceljenner.com")
                            .AllowCredentials()));

            services.AddSignalR()
                .AddJsonProtocol(options =>
                {
                    options.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                });
            
            services.AddApiVersioningAndExplorer();
            services.AddOpenApi();

            services.AddBackgroundScheduler()
                .AddJob<CleanOldLobbiesJob>(_configuration);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseOpenApi();
            app.UseExceptionHandler("/error");
            app.UseMiddleware<LoggingMiddleware>();
            
            // app.UseHttpsRedirection();

            app.UseStaticFiles();
            app.UseRouting();
            app.UseCors();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHub<TriviaGameHub>("/triviaGameServer");
            });
        }
    }
}