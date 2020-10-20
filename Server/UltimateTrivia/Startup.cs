using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using BackgroundScheduler;
using IdentityServer4.Models;
using Microsoft.AspNetCore.ApiAuthorization.IdentityServer;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using UltimateTrivia.Application;
using UltimateTrivia.BackgroundJobs;
using UltimateTrivia.Database.Game;
using UltimateTrivia.Database.Identity;
using UltimateTrivia.Hubs;
using UltimateTrivia.Middlewares;
using UltimateTrivia.Services;

namespace UltimateTrivia
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
            services.AddTransient<UserRepository>();
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
                .AddDbContextCheck<ApplicationDbContext>()
                .AddDbContextCheck<IdentityDbContext>();
            
            services.AddControllersWithViews()
                .AddNewtonsoftJson(options => // TODO: change custom serializer to system.text.json
                {
                    options.SerializerSettings.Converters.Add(new StringEnumConverter());
                    options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                    options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                }).AddJsonSerializer();

            services.AddRazorPages();
            
            services.AddCors(options => options
                    .AddDefaultPolicy(builder => builder
                            .WithOrigins(
                                "http://localhost:1234", 
                                "https://localhost:1234", 
                                "http://quiz.fabiobaser.de",
                                "https://quiz.fabiobaser.de",
                                "http://localhost:5000", 
                                "https://localhost:5001",
                                "http://quiz.fabiobaser.de:5000",
                                "https://quiz.fabiobaser.de:5001")
                            .AllowAnyHeader()
                            .AllowAnyMethod()
                            .AllowCredentials()));

            services.AddSignalR()
                .AddJsonProtocol(options =>
                {
                    options.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                });

            services.AddSingleton<IUserIdProvider, DefaultUserIdProvider>();
            
            services.AddApiVersioningAndExplorer();
            services.AddOpenApi();

            services.AddBackgroundScheduler()
                .AddJob<CleanOldLobbiesJob>(_configuration);

            services.AddDbContext<IdentityDbContext>(options =>
            {
                options.UseNpgsql(_configuration.GetConnectionString("DefaultConnection"));
            });
            
            services.AddDefaultIdentity<IdentityUser>(options =>
                {
                    options.SignIn.RequireConfirmedAccount = true;
                    options.Password.RequireNonAlphanumeric = false;
                })
                .AddEntityFrameworkStores<IdentityDbContext>();

            services.AddIdentityServer(options =>
                {
                    options.Events.RaiseErrorEvents = true;
                    options.Events.RaiseInformationEvents = true;
                    options.Events.RaiseFailureEvents = true;
                    options.Events.RaiseSuccessEvents = true;
                })
                .AddApiAuthorization<IdentityUser, IdentityDbContext>(options =>
                {
                    options.Clients.Add(new Client
                    {
                        Enabled   = true,
                        AllowedScopes = new List<string> {"UltimateTriviaAPI"},
                        ClientId = "Swagger",
                        ClientName = "Swagger",
                        RedirectUris = new List<string>
                        {
                            "https://localhost:5001/swagger/oauth2-redirect.html", 
                            "https://quiz.fabiobaser.de:5001/swagger/oauth2-redirect.html"
                        },
                        AllowedGrantTypes = GrantTypes.Code,
                        RequireClientSecret = false,
                        RequirePkce = true,
                        AllowAccessTokensViaBrowser = true,
                        RequireConsent = false
                    });
                    
                    options.Clients.AddSPA("ultimate-trivia-client", options =>
                    {
                        options.WithScopes("openid", "profile", "email", "UltimateTriviaAPI")
                            .WithoutClientSecrets()
                            .WithRedirectUri("https://localhost:1234/signin-oidc")
                            .WithRedirectUri("https://quiz.fabiobaser.de/signin-oidc")
                            .WithLogoutRedirectUri("https://localhost:1234/signout-oidc")
                            .WithLogoutRedirectUri("https://quiz.fabiobaser.de/signout-oidc");
                    });
                    
                    // options.Clients["ultimate-trivia-client"].Claims.Add(ClaimTypes.NameIdentifier);
                
                    options.IdentityResources.AddEmail();
                });

            services.AddAuthentication()
                .AddIdentityServerJwt()
                .AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
                {
                    options.ClientId = "116681063698-gnuds2j676l6rutblab6o8umuqs6m8ra.apps.googleusercontent.com";
                    options.ClientSecret = "RjLr2mkyMjiEXRRYaV8TKmMp";

                    options.CorrelationCookie.SameSite = SameSiteMode.None;
                })
                .AddMicrosoftAccount(MicrosoftAccountDefaults.AuthenticationScheme, options =>
                {
                    options.ClientId = "627c7829-05cd-425b-9723-4d530049f57b";
                    options.ClientSecret = "934145e9-6b75-43e0-9096-425cc6e66712";
                    options.CorrelationCookie.SameSite = SameSiteMode.None;
                });

            // services.PostConfigure<JwtBearerOptions>(IdentityServerJwtConstants.IdentityServerJwtBearerScheme ,options =>
            // {
            //     options.Events.OnMessageReceived = async context =>
            //     {
            //         var token = context.Request.Query["Token"].FirstOrDefault();
            //         if (token != null)
            //         {
            //             context.Token = token;
            //         }
            //     };
            // });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseCors();
            app.UseHttpsRedirection();

            app.UseOpenApi();

            app.UseStaticFiles();

            app.UseWhen(context => context.Request.Path.StartsWithSegments(new PathString("/api")), branch => 
            {
                branch.UseExceptionHandler("/api/v1/error");
                branch.UseMiddleware<LoggingMiddleware>();

                branch.UseRouting();
                branch.UseAuthentication();
                branch.UseAuthorization();

                branch.UseEndpoints(endpoints =>
                {
                    endpoints.MapControllerRoute(
                        name: "default",
                        pattern: "{controller}/{action=Index}/{id?}");
                });
            });
           
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            // TODO: HACK: provide bearer token from query - websocket cant set auth header - change to JwtBearerEvents
            app.Use(async (context, next) =>
            {
                var logger = context.RequestServices.GetService<ILogger<Startup>>();
                
                var token = context.Request.Query["access_token"].FirstOrDefault();
                var path = context.Request.Path;
                if (!string.IsNullOrWhiteSpace(token) && path.StartsWithSegments("/triviaGameServer"))
                {
                    logger.LogInformation("detected bearer token in query - moved to header");
                    context.Request.Headers.Add("Authorization", "Bearer " + token);
                }

                await next();
            });
            
            app.UseRouting();
            app.UseAuthentication();
            app.UseIdentityServer();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller}/{action=Index}/{id?}");
                endpoints.MapHub<TriviaGameHub>("/triviaGameServer");
                endpoints.MapRazorPages();
            });
        }
    }
}