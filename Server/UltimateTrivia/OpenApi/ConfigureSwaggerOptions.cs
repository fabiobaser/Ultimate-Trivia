using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace UltimateTrivia.OpenApi
{
    public class ConfigureSwaggerOptions : IConfigureOptions<SwaggerGenOptions>
    {
        readonly IApiVersionDescriptionProvider _provider;

        public ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider) => this._provider = provider;

        public void Configure(SwaggerGenOptions options)
        {
            foreach(var description in _provider.ApiVersionDescriptions)
            {
                options.SwaggerDoc(description.GroupName, CreateInfoForApiVersion(description));
            }
            
            // options.AddSecurityDefinition("oauth2 localhost", new OpenApiSecurityScheme
            // {
            //     Type = SecuritySchemeType.OAuth2,
            //     OpenIdConnectUrl = new Uri("https://localhost:5001/.well-known/openid-configuration"),
            //     Name = "Authorization",
            //     In = ParameterLocation.Header,
            //     Scheme = JwtBearerDefaults.AuthenticationScheme,
            //     BearerFormat = "JWT",
            //     Flows = new OpenApiOAuthFlows
            //     {
            //         AuthorizationCode = new OpenApiOAuthFlow
            //         {
            //             AuthorizationUrl = new Uri("https://localhost:5001/connect/authorize"),
            //             TokenUrl = new Uri("https://localhost:5001/connect/token"),
            //             Scopes = new Dictionary<string, string>
            //             {
            //                 // ["profile"] = "Profileinformation",
            //                 // ["openid"] = "OpenId",
            //                 // ["email"] = "E-Mail",
            //                 ["UltimateTriviaAPI"] = "Ultimate Trivia Api"
            //             }
            //         }
            //     }
            // });
            
            options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.OAuth2,
                Name = "Authorization",
                In = ParameterLocation.Header,
                Scheme = JwtBearerDefaults.AuthenticationScheme,
                BearerFormat = "JWT",
                Flows = new OpenApiOAuthFlows
                {
                    AuthorizationCode = new OpenApiOAuthFlow
                    {
                        AuthorizationUrl = new Uri("https://marceljenner.com:5001/connect/authorize"),
                        TokenUrl = new Uri("https://marceljenner.com:5001/connect/token"),
                        Scopes = new Dictionary<string, string>
                        {
                            ["UltimateTriviaAPI"] = "Ultimate Trivia Api"
                        }
                    }
                }
            });
        }

        static OpenApiInfo CreateInfoForApiVersion(ApiVersionDescription description)
        {
            var info = new OpenApiInfo
            {
                Title = "Best UltimateTrivia Game EVER",
                Version = description.ApiVersion.ToString(),
                Description = "hier könnte deine Beschreibung stehen",
                Contact = new OpenApiContact() { Name = "Marcel Jenner", Email = "developer@marceljenner.com" },
                License = new OpenApiLicense() { Name = "MIT", Url = new Uri("https://opensource.org/licenses/MIT") }
            };

            if (description.IsDeprecated)
            {
                info.Description += " This API version has been deprecated.";
            }

            return info;
        }
    }
}