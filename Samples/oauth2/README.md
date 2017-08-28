# ASP.NET Core OAuth2 Sample

This sample demonstrates how you can configure the standard OAuth2 middleware to authenticate users of an ASP.NET Core MVC application using Auth0. 

## Requirements

* .[NET Core 2.0 SDK](https://www.microsoft.com/net/download/core)

## To run this project

1. Ensure that you have replaced the [appsettings.json](appsettings.json) file with the values for your Auth0 account.

2. Run the application from the command line:

    ```bash
    dotnet run
    ```

3. Go to `http://localhost:5000` in your web browser to view the website.


## Important Snippets

### 1. Configure your Auth0 application

Go to the Auth0 Dashboard and ensure that you add the URL http://localhost:5000/signin-auth0 to your list of callback URLs

### 2. Configure Authentication Services

In the ConfigureServices of your Startup class, ensure that you add the authentication services:

```csharp
// Startup.cs

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
```

### 3. Configure the Authentication Middleware

In the Configure method of your Startup class, register the Authentication middleware by calling `UseAuthentication`:

```csharp
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
```

### 4. Handle the login and logout routes

The cookie middleware will redirect users to the `account/login` and `account/logout` paths respectively to log users in or out. We need to add an `AccountController` class with actions to handle these routes:

```csharp
public class AccountController: Controller
{
    private readonly IConfiguration _configuration;

    public AccountController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task Login(string returnUrl = "/")
    {
        await HttpContext.ChallengeAsync("Auth0", new AuthenticationProperties() { RedirectUri = returnUrl });
    }

    [Authorize]
    public async Task Logout()
    {
        // Sign the user out of the cookie authentication middleware (i.e. it will clear the local session cookie)
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        // Construct the post-logout URL (i.e. where we'll tell Auth0 to redirect after logging the user out)
        var request = HttpContext.Request;
        string postLogoutUri = request.Scheme + "://" + request.Host + request.PathBase + Url.Action("Index", "Home");

        // Redirect to the Auth0 logout endpoint in order to log out of Auth0
        string logoutUri = $"https://{_configuration["Auth0:Domain"]}/v2/logout?client_id={_configuration["Auth0:ClientId"]}&returnTo={Uri.EscapeDataString(postLogoutUri)}";
        HttpContext.Response.Redirect(logoutUri);
    }
}
```
