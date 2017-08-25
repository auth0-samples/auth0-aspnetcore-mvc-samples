using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;

namespace AspNetCoreOAuth2Sample
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add authentication services
            services.AddAuthentication(options => {
                options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddCookie()
            .AddOAuth("Auth0", options => {
                // Configure the Auth0 Client ID and Client Secret
                options.ClientId = Configuration["Auth0:ClientId"];
                options.ClientSecret = Configuration["Auth0:ClientSecret"];

                // Set the callback path, so Auth0 will call back to http://localhost:5000/signin-auth0 
                // Also ensure that you have added the URL as an Allowed Callback URL in your Auth0 dashboard 
                options.CallbackPath = new PathString("/signin-auth0");

                // Configure the Auth0 endpoints                
                options.AuthorizationEndpoint = $"https://{Configuration["Auth0:Domain"]}/authorize";
                options.TokenEndpoint = $"https://{Configuration["Auth0:Domain"]}/oauth/token";
                options.UserInformationEndpoint = $"https://{Configuration["Auth0:Domain"]}/userinfo";

                // To save the tokens to the Authentication Properties we need to set this to true
                // See code in OnTicketReceived event below to extract the tokens and save them as Claims
                options.SaveTokens = true;

                // Set scope to openid. See https://auth0.com/docs/scopes
                options.Scope.Clear();
                options.Scope.Add("openid");
                options.Scope.Add("profile");
                
                options.Events = new OAuthEvents
                {
                    // When creating a ticket we need to manually make the call to the User Info endpoint to retrieve the user's information,
                    // and subsequently extract the user's ID and email adddress and store them as claims
                    OnCreatingTicket = async context =>
                    {
                        // Retrieve user info
                        var request = new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);
                        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                        var response = await context.Backchannel.SendAsync(request, context.HttpContext.RequestAborted);
                        response.EnsureSuccessStatusCode();

                        // Extract the user info object
                        var user = JObject.Parse(await response.Content.ReadAsStringAsync());

                        // Add the Name Identifier claim
                        var userId = user.Value<string>("sub");
                        if (!string.IsNullOrEmpty(userId))
                        {
                            context.Identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, userId, ClaimValueTypes.String, context.Options.ClaimsIssuer));
                        }

                        // Add the Name claim
                        var email = user.Value<string>("name");
                        if (!string.IsNullOrEmpty(email))
                        {
                            context.Identity.AddClaim(new Claim(ClaimsIdentity.DefaultNameClaimType, email, ClaimValueTypes.String, context.Options.ClaimsIssuer));
                        }
                    }
                };         
            });

            // Add framework services.
            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseAuthentication();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
