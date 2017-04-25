# ASP.NET Core OAuth2 Sample

This sample demonstrates how you can configure the standard OAuth2 middleware to authenticate users of an ASP.NET Core MVC application using Auth0. 

## 1. Configure your Auth0 application

Go to the Auth0 Dashboard and ensure that you add the URL http://localhost:5000/signin-auth0 to your list of callback URLs

## 2. Add the cookie and OAuth NuGet packages

```
Install-Package Microsoft.AspNetCore.Authentication.Cookies
Install-Package Microsoft.AspNetCore.Authentication.OAuth
```

## 3. Configure Authentication Services

In the ConfigureServices of your Startup class, ensure that you add the authentication services:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    // Add authentication services
    services.AddAuthentication(options => options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme);

    // Add framework services.
    services.AddMvc();

    // Add functionality to inject IOptions<T>
    services.AddOptions();

    // Add the Auth0 Settings object so it can be injected
    services.Configure<Auth0Settings>(Configuration.GetSection("Auth0"));
}
```

## 4. Configure the cookie and OAuth middleware

In the Configure method of your Startup class, register the Cookie and OAuth middleware:

```csharp
// Add the cookie middleware
app.UseCookieAuthentication(new CookieAuthenticationOptions
{
    AutomaticAuthenticate = true,
    AutomaticChallenge = true
});

// Add the OAuth2 middleware
app.UseOAuthAuthentication(new OAuthOptions
{
    // We need to specify an Authentication Scheme
    AuthenticationScheme = "Auth0",

    // Configure the Auth0 Client ID and Client Secret
    ClientId = auth0Settings.Value.ClientId,
    ClientSecret = auth0Settings.Value.ClientSecret,

    // Set the callback path, so Auth0 will call back to http://localhost:5000/signin-auth0 
    // Also ensure that you have added the URL as an Allowed Callback URL in your Auth0 dashboard 
    CallbackPath = new PathString("/signin-auth0"),

    // Configure the Auth0 endpoints                
    AuthorizationEndpoint = $"https://{auth0Settings.Value.Domain}/authorize",
    TokenEndpoint = $"https://{auth0Settings.Value.Domain}/oauth/token",
    UserInformationEndpoint = $"https://{auth0Settings.Value.Domain}/userinfo",

    // To save the tokens to the Authentication Properties we need to set this to true
    // See code in OnTicketReceived event below to extract the tokens and save them as Claims
    SaveTokens = true,

    // Set scope to openid. See https://auth0.com/docs/scopes
    Scope = { "openid" },
    
    Events = new OAuthEvents
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
            var userId = user.Value<string>("user_id");
            if (!string.IsNullOrEmpty(userId))
            {
                context.Identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, userId, ClaimValueTypes.String, context.Options.ClaimsIssuer));
            }

            // Add the Name claim
            var email = user.Value<string>("email");
            if (!string.IsNullOrEmpty(email))
            {
                context.Identity.AddClaim(new Claim(ClaimsIdentity.DefaultNameClaimType, email, ClaimValueTypes.String, context.Options.ClaimsIssuer));
            }
        }
    }
});
```

## 5. Handle the login and logout routes

The cookie middleware will redirect users to the `account/login` and `account/logout` paths respectively to log users in or out. We need to add an `AccountController` class with actions to handle these routes:

```csharp
public class AccountController: Controller
{
    private readonly IOptions<Auth0Settings> _auth0Settings;

    public AccountController(IOptions<Auth0Settings> auth0Settings)
    {
        _auth0Settings = auth0Settings;
    }

    public IActionResult Login(string returnUrl = "/")
    {
        return new ChallengeResult("Auth0", new AuthenticationProperties() { RedirectUri = returnUrl });
    }

    [Authorize]
    public async Task Logout()
    {
        // Sign the user out of the cookie authentication middleware (i.e. it will clear the local session cookie)
        await HttpContext.Authentication.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        // Construct the post-logout URL (i.e. where we'll tell Auth0 to redirect after logging the user out)
        var request = HttpContext.Request;
        string postLogoutUri = request.Scheme + "://" + request.Host + request.PathBase + Url.Action("Index", "Home");

        // Redirect to the Auth0 logout endpoint in order to log out of Auth0
        string logoutUri = $"https://{_auth0Settings.Value.Domain}/v2/logout?client_id={_auth0Settings.Value.ClientId}&returnTo={Uri.EscapeDataString(postLogoutUri)}";
        HttpContext.Response.Redirect(logoutUri);
    }
}
```

# Running the application

To run this sample you can fork and clone this repo.

Be sure to update the appsettings.json with your Auth0 settings:

    {
        "Auth0": {
            "domain": "Your Auth0 domain",
            "clientId": "Your Auth0 Client Id",
            "clientSecret": "Your Auth0 Client Secret"
        } 
    }

Then, restore the NuGet and Bower packages and run the application:

```
# Install the dependencies
bower install
dotnet restore

# Run
dotnet run
```

You can shut down the web server manually by pressing Ctrl-C.
