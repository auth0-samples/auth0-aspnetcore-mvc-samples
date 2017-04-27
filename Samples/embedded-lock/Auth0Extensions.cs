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