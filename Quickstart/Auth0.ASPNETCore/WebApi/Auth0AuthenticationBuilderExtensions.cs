using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

namespace Auth0.ASPNETCore.WebApi
{
    public static class Auth0AuthenticationBuilderExtensions
    {
        public static AuthenticationBuilder AddAuth0WebApi(this AuthenticationBuilder builder, Action<Auth0Options> configureOptions)
        {
            var auth0Options = new Auth0Options();

            configureOptions(auth0Options);

            builder.AddJwtBearer(options => ConfigureJwtBearer(options, auth0Options));

            return builder;
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
