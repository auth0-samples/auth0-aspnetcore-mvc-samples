using System;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace SampleMvcApp
{
    public static class Auth0Extensions
    {
        private static readonly RandomNumberGenerator CryptoRandom = RandomNumberGenerator.Create();
        private const string CorrelationPrefix = ".AspNetCore.Correlation.";
        private const string CorrelationProperty = ".xsrf";
        private const string CorrelationMarker = "N";
        private const string NonceProperty = "N";

        public static IServiceCollection AddAuth0(this IServiceCollection services, string domain, string clientId, string clientSecret)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            // Register Authentication Services 
            services.AddAuthentication(
                options => options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme);

            // Configure OIDC Options
            services.Configure<OpenIdConnectOptions>(options =>
            {
                options.AutomaticAuthenticate = false;
                options.AutomaticChallenge = false;

                // We need to specify an Authentication Scheme
                options.AuthenticationScheme = "Auth0";

                // Set the authority to your Auth0 domain
                options.Authority = $"https://{domain}";

                // Configure the Auth0 Client ID and Client Secret
                options.ClientId = clientId;
                options.ClientSecret = clientSecret;

                // Set response type to code
                options.ResponseType = "code";

                // Set the callback path, so Auth0 will call back to http://your_domain/signin-auth0 
                // Also ensure that you have added the URL as an Allowed Callback URL in your Auth0 dashboard 
                options.CallbackPath = new PathString("/signin-auth0");

                // Configure the Claims Issuer to be Auth0
                options.ClaimsIssuer = "Auth0";

                options.Events = new OpenIdConnectEvents
                {
                    OnTicketReceived = context =>
                    {
                        // Get the ClaimsIdentity
                        var identity = context.Principal.Identity as ClaimsIdentity;
                        if (identity != null)
                        {
                            // Add the Name ClaimType. This is required if we want User.Identity.Name to actually return something!
                            if (!context.Principal.HasClaim(c => c.Type == ClaimTypes.Name) &&
                                            identity.HasClaim(c => c.Type == "name"))
                                identity.AddClaim(new Claim(ClaimTypes.Name, identity.FindFirst("name").Value));
                        }

                        return Task.FromResult(0);
                    }
                };
            });

            return services;
        }

        public static IApplicationBuilder UseAuth0(this IApplicationBuilder app)
        {
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AutomaticAuthenticate = true,
                AutomaticChallenge = true
            });

            // Configure OIDC options
            var options = app.ApplicationServices.GetRequiredService<IOptions<OpenIdConnectOptions>>();
            app.UseOpenIdConnectAuthentication(options.Value);

            return app;
        }

        private static string BuildRedirectUri(HttpRequest request, PathString redirectPath)
        {
            return request.Scheme + "://" + request.Host + request.PathBase + redirectPath;
        }

        private static void GenerateCorrelationId(HttpContext httpContext, OpenIdConnectOptions options, AuthenticationProperties properties)
        {
            if (properties == null)
            {
                throw new ArgumentNullException(nameof(properties));
            }

            var bytes = new byte[32];
            CryptoRandom.GetBytes(bytes);
            var correlationId = Base64UrlTextEncoder.Encode(bytes);

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = httpContext.Request.IsHttps,
                Expires = properties.ExpiresUtc
            };

            properties.Items[CorrelationProperty] = correlationId;

            var cookieName = CorrelationPrefix + options.AuthenticationScheme + "." + correlationId;

            httpContext.Response.Cookies.Append(cookieName, CorrelationMarker, cookieOptions);
        }

        public static LockContext GenerateLockContext(this HttpContext httpContext, OpenIdConnectOptions options, string returnUrl = null)
        {
            LockContext lockContext = new LockContext();

            // Set the options
            lockContext.ClientId = options.ClientId;

            // retrieve the domain from the authority
            Uri authorityUri;
            if (Uri.TryCreate(options.Authority, UriKind.Absolute, out authorityUri))
            {
                lockContext.Domain = authorityUri.Host;
            }

            // Set the redirect
            string callbackUrl = BuildRedirectUri(httpContext.Request, options.CallbackPath);
            lockContext.CallbackUrl = callbackUrl;

            // Add the nonce.
            var nonce = options.ProtocolValidator.GenerateNonce();
            httpContext.Response.Cookies.Append(
                OpenIdConnectDefaults.CookieNoncePrefix + options.StringDataFormat.Protect(nonce),
                NonceProperty,
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = httpContext.Request.IsHttps,
                    Expires = DateTime.UtcNow + options.ProtocolValidator.NonceLifetime
                });
            lockContext.Nonce = nonce;

            // Since we are handling the 1st leg of the Auth (redirecting to /authorize), we need to generate the correlation ID so the 
            // OAuth middleware can validate it correctly once it picks up from the 2nd leg (receiving the code)
            var properties = new AuthenticationProperties()
            {
                ExpiresUtc = options.SystemClock.UtcNow.Add(options.RemoteAuthenticationTimeout),
                RedirectUri = returnUrl ?? "/"
            };
            properties.Items[OpenIdConnectDefaults.RedirectUriForCodePropertiesKey] = callbackUrl;
            GenerateCorrelationId(httpContext, options, properties);

            // Generate State
            lockContext.State = Uri.EscapeDataString(options.StateDataFormat.Protect(properties));

            // return the Lock context
            return lockContext;
        }
    }
}