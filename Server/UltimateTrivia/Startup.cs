using System.Collections.Generic;
using System.Text.Json;
using BackgroundScheduler;
using IdentityServer4.EntityFramework.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SpaServices.ReactDevelopmentServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
            services.AddSpaStaticFiles(configuration =>
            {
                configuration.RootPath = "ClientApp/dist";
            });
            
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
                                "https://localhost:5001",
                                "http://marceljenner.com:5000",
                                "https://marceljenner.com:5001")
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
                })
                .AddApiAuthorization<IdentityUser, IdentityDbContext>(options =>
                {
                    options.Clients.AddIdentityServerSPA("Swagger", builder =>
                    {
                        builder
                            .WithRedirectUri("https://localhost:5001/swagger/oauth2-redirect.html")
                            .WithRedirectUri("https://editor.swagger.io/oauth2-redirect.html")
                            .WithScopes("openid", "profile", "email")
                            .WithoutClientSecrets();
                    });
                });

            services.AddAuthentication()
                .AddIdentityServerJwt()
                .AddGoogle(GoogleDefaults.AuthenticationScheme,options =>
                {
                    options.ClientId = "116681063698-gnuds2j676l6rutblab6o8umuqs6m8ra.apps.googleusercontent.com";
                    options.ClientSecret = "RjLr2mkyMjiEXRRYaV8TKmMp";
            
                    options.CorrelationCookie.SameSite = SameSiteMode.None;
                });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseCors();
            app.UseHttpsRedirection();

            app.UseOpenApi();

            app.UseStaticFiles();
            app.UseSpaStaticFiles();

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
                app.UseHsts();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }
            
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
            
            app.UseSpa(spa =>
            {
                spa.Options.SourcePath = "ClientApp";

                if (env.IsDevelopment())
                {
                    spa.UseReactDevelopmentServer(npmScript: "start");
                }
            });
        }
    }
}