using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Trivia.Constants;

namespace Trivia.OpenApi
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
            
            // options.AddSecurityDefinition(GoogleDefaults.AuthenticationScheme, new OpenApiSecurityScheme
            // {
            //     Type = SecuritySchemeType.OAuth2,
            //     Name = "Authorization",
            //     In = ParameterLocation.Header,
            //     Scheme = JwtBearerDefaults.AuthenticationScheme,
            //     BearerFormat = "JWT",
            //     OpenIdConnectUrl = new Uri("https://accounts.google.com/.well-known/openid-configuration"),
            //     Flows = new OpenApiOAuthFlows
            //     {
            //         AuthorizationCode = new OpenApiOAuthFlow
            //         {
            //             AuthorizationUrl = new Uri(GoogleDefaults.AuthorizationEndpoint),
            //             TokenUrl = new Uri(GoogleDefaults.TokenEndpoint),
            //             Scopes = new Dictionary<string, string>
            //             {
            //                 ["openid"] = "",
            //                 ["email"] = "",
            //                 ["profile"] = ""
            //             }
            //         }
            //     }
            // });
        }

        static OpenApiInfo CreateInfoForApiVersion(ApiVersionDescription description)
        {
            var info = new OpenApiInfo
            {
                Title = "Best Trivia Game EVER",
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