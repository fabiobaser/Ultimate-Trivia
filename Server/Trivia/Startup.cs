using System.Text.Json;
using BackgroundScheduler;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Trivia.Application;
using Trivia.BackgroundJobs;
using Trivia.Constants;
using Trivia.Database;
using Trivia.Hubs;
using Trivia.Middlewares;
using Trivia.Services;

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
            services.AddTransient<PlayerManager>();
            services.AddTransient<QuestionRepository>();
            services.AddTransient<ICurrentUserService, CurrentUserService>();
            
            services.AddSingleton<PlayerStore>();
            services.AddSingleton<LobbyStore>();
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
                            .WithOrigins(
                                "http://localhost:1234", 
                                "https://localhost:1234", 
                                "http://marceljenner.com:1234",
                                "https://marceljenner.com:1234",
                                "http://localhost:5000", 
                                "https://localhost:5000", 
                                "http://marceljenner.com:5000",
                                "https://marceljenner.com:5000")
                            .AllowAnyHeader()
                            .AllowAnyMethod()
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

            services.AddAuthentication(options =>
                {
                    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
                })
                .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
                {
                    options.Cookie.Name = CookieNames.Identity;
                })
                .AddGoogle(GoogleDefaults.AuthenticationScheme,options =>
                {
                    options.ClientId = "116681063698-gnuds2j676l6rutblab6o8umuqs6m8ra.apps.googleusercontent.com";
                    options.ClientSecret = "RjLr2mkyMjiEXRRYaV8TKmMp";

                    options.CorrelationCookie.SameSite = SameSiteMode.None;
                    
                    options.Events.OnRemoteFailure = async context =>
                    {

                    };
                });

            services.AddAuthorization(options =>
            {
                options.AddPolicy("admin", builder =>
                {
                    builder.RequireClaim(Claims.UltimateTriviaAdmin);
                });
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseCors();
            app.UseHttpsRedirection();

            app.UseOpenApi();

            app.UseExceptionHandler("/api/error");
            app.UseMiddleware<LoggingMiddleware>();

            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseStaticFiles();
            
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHub<TriviaGameHub>("/triviaGameServer");
            });
        }
    }
}