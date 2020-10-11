using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Trivial.Application;
using Trivial.Database;
using Trivial.Hubs;
using Trivial.Middlewares;

namespace Trivial
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
            services.AddSingleton<LobbyManager>();

            services.AddUtils();
            services.AddHttpContextAccessor();

            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlite(_configuration.GetConnectionString("DefaultConnection"));
            });
            
            services.AddHealthChecks()
                .AddDbContextCheck<ApplicationDbContext>();
            
            services.AddControllers()
                .AddNewtonsoftJson(options =>
                {
                    options.SerializerSettings.Converters.Add(new StringEnumConverter());
                    options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                    options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                }).AddJsonSerializer();

            services.AddCors(options =>
                options
                    .AddDefaultPolicy(builder =>
                        builder
                            .WithOrigins("http://localhost:5000")
                            .AllowCredentials()));
            
            services.AddSignalR();
            
            services.AddApiVersioningAndExplorer();
            services.AddOpenApi();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseOpenApi();
            app.UseExceptionHandler("/error");
            app.UseMiddleware<LoggingMiddleware>();
            
            app.UseHttpsRedirection();

            app.UseStaticFiles();
            app.UseRouting();
            app.UseCors();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHub<TrivialGameHub>("/trivialGameHub");
            });
        }
    }
}