using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace Auth0.ASPNETCore.WebApi
{

    public static class Auth0AuthenticationBuilderExtensions
    {
        public static IServiceCollection AddAuth0WebApi(this IServiceCollection services, Action<Auth0Options> configureOptions)
        {
            var auth0Options = new Auth0Options();

            configureOptions(auth0Options);

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options => ConfigureJwtBearer(options, auth0Options));

            services.AddAuthorization(options =>
            {
                foreach(var scope in auth0Options.Scopes)
                {
                    options.AddPolicy(scope, policy => policy.Requirements.Add(new HasScopeRequirement(scope, $"https://{auth0Options.Domain}/")));
                }
            });

            services.AddSingleton<IAuthorizationHandler, HasScopeHandler>();

            return services;
        }

        private static void ConfigureJwtBearer(JwtBearerOptions jwtBearerOptions, Auth0Options auth0Options)
        {
            jwtBearerOptions.Authority = $"https://{auth0Options.Domain}/";
            jwtBearerOptions.Audience = auth0Options.Audience;
            jwtBearerOptions.TokenValidationParameters = new TokenValidationParameters
            {
                ValidAudience = auth0Options.Audience,
                NameClaimType = ClaimTypes.NameIdentifier
            };
        }
    }
}
