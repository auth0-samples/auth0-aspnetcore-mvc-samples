using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace Auth0.ASPNETCore.MVC
{
    public static class Auth0AuthenticationBuilderExtensions
    {
        public static AuthenticationBuilder AddAuth0MVC(this AuthenticationBuilder builder, Action<Auth0Options> configureOptions)
        {
            var auth0Options = new Auth0Options();

            configureOptions(auth0Options);

            builder.AddCookie(op => op.Events = new CookieAuthenticationEvents
            {
                OnValidatePrincipal = async context =>
                {
                    var now = DateTimeOffset.UtcNow;
                    var timeElapsed = now.Subtract(context.Properties.IssuedUtc.Value);
                    var timeRemaining = context.Properties.ExpiresUtc.Value.Subtract(now);

                    if (timeElapsed > timeRemaining)
                    {

                        var identity = (ClaimsIdentity)context.Principal.Identity;

                        // TODO: These should not go in a Claim.
                        var accessTokenClaim = identity.FindFirst("access_token");
                        var refreshTokenClaim = identity.FindFirst("refresh_token");
                        var refreshToken = refreshTokenClaim.Value;

                        // Get New Tokens

                        context.ShouldRenew = true;

                    }
                }
            });
            builder.AddOpenIdConnect("Auth0", options => ConfigureOpenIdConnect(options, auth0Options));

            return builder;
        }

        private static Func<RedirectContext, Task> CreateOnRedirectToIdentityProvider(Auth0Options auth0Options)
        {
            var auth0AuthorizeOptions = auth0Options.AuthorizeOptions;

            return (context) =>
            {
                foreach (var extraParam in GetAuthorizeParameters(auth0AuthorizeOptions, context.Properties.Items))
                {
                    context.ProtocolMessage.SetParameter(extraParam.Key, extraParam.Value);
                }

                return Task.CompletedTask;
            };
        }

        private static IDictionary<string, string> GetAuthorizeParameters(Auth0AuthorizeOptions auth0AuthorizeOptions, IDictionary<string, string> authSessionItems)
        {
            var parameters = new Dictionary<string, string>();

            if (!string.IsNullOrEmpty(auth0AuthorizeOptions.Audience))
            {
                parameters["audience"] = auth0AuthorizeOptions.Audience;
            }

            if (!string.IsNullOrEmpty(auth0AuthorizeOptions.Organization))
            {
                parameters["organization"] = auth0AuthorizeOptions.Organization;
            }

            if (auth0AuthorizeOptions.ExtraParameters != null)
            {
                foreach (var extraParam in auth0AuthorizeOptions.ExtraParameters)
                {
                    parameters[extraParam.Key] = extraParam.Value;
                }
            }

            var authSessionItemKeys = new List<string> { "organization" };

            foreach (var itemKey in authSessionItemKeys)
            {
                if (authSessionItems.ContainsKey(itemKey))
                    parameters[itemKey] = authSessionItems[itemKey];
            }


            return parameters;
        }


        private static Func<RedirectContext, Task> CreateOnRedirectToIdentityProviderForSignOut(Auth0Options auth0Options)
        {
            return (context) =>
            {
                var logoutUri = $"https://{auth0Options.Domain}/v2/logout?client_id={auth0Options.ClientId}";
                var postLogoutUri = context.Properties.RedirectUri;

                if (!string.IsNullOrEmpty(postLogoutUri))
                {
                    if (postLogoutUri.StartsWith("/"))
                    {
                        // transform to absolute
                        var request = context.Request;
                        postLogoutUri = request.Scheme + "://" + request.Host + request.PathBase + postLogoutUri;
                    }

                    logoutUri += $"&returnTo={ Uri.EscapeDataString(postLogoutUri)}";
                }

                context.Response.Redirect(logoutUri);
                context.HandleResponse();

                return Task.CompletedTask;
            };
        }

        private static Func<TokenValidatedContext, Task> CreateOnTokenValidated()
        {
            return (context) =>
            {
                var identity = (ClaimsIdentity)context.Principal.Identity;

                identity.AddClaims(new[]
                {
                    new Claim("access_token", context.TokenEndpointResponse.AccessToken)
                });

                // so that we don't issue a session cookie but one with a fixed expiration
                // context.Properties.IsPersistent = true;

                // align expiration of the cookie with expiration of the
                // access token
                // var accessToken = new JwtSecurityToken(x.TokenEndpointResponse.AccessToken);
                // x.Properties.ExpiresUtc = accessToken.ValidTo;
                return Task.CompletedTask;
            };
        }

        private static void ConfigureOpenIdConnect(OpenIdConnectOptions oidcOptions, Auth0Options auth0Options)
        {
            oidcOptions.Authority = $"https://{auth0Options.Domain}";
            oidcOptions.ClientId = auth0Options.ClientId;
            oidcOptions.ClientSecret = auth0Options.ClientSecret;
            oidcOptions.ResponseType = OpenIdConnectResponseType.Code;
            oidcOptions.SaveTokens = true;

            oidcOptions.Scope.Clear();

            if (auth0Options.Scope != null)
            {
                foreach (var scope in auth0Options.Scope)
                {
                    oidcOptions.Scope.Add(scope);
                }
            }
            else
            {
                oidcOptions.Scope.Add("openid");
                oidcOptions.Scope.Add("profile");
                oidcOptions.Scope.Add("email");
            }

            if (auth0Options.UseRefreshTokens && !oidcOptions.Scope.Any(s => s == "offline_access"))
            {
                oidcOptions.Scope.Add("offline_access");
            }

            oidcOptions.CallbackPath = new PathString(auth0Options.CallbackPath ?? "/callback");
            oidcOptions.ClaimsIssuer = "Auth0";
            oidcOptions.UseTokenLifetime = true;

            oidcOptions.TokenValidationParameters = new TokenValidationParameters
            {
                ValidAudience = auth0Options.ClientId,
                NameClaimType = "name",
                RoleClaimType = "https://schemas.quickstarts.com/roles"
            };

            oidcOptions.Events = new OpenIdConnectEvents
            {
                OnRedirectToIdentityProviderForSignOut = CreateOnRedirectToIdentityProviderForSignOut(auth0Options),
                OnRedirectToIdentityProvider = CreateOnRedirectToIdentityProvider(auth0Options),
                OnTokenValidated = CreateOnTokenValidated()
            };
        }
    }
}
