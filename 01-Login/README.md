# Login

This example shows how to add ***Login/SignUp*** to your application using the hosted version of the `Lock` widget.

You can read a quickstart for this sample [here](https://auth0.com/docs/quickstart/webapp/aspnet-core/01-login). 

## Getting Started

To run this quickstart you can fork and clone this repo.

Be sure to update the `appsettings.json` with your Auth0 settings:

```json
{
  "Auth0": {
    "Domain": "Your Auth0 domain",
    "ClientId": "Your Auth0 Client Id",
    "ClientSecret": "Your Auth0 Client Secret",
    "CallbackUrl": "http://localhost:5000/signin-auth0"
  } 
}
```

Then restore the NuGet packages and run the application:

```bash
# Install the dependencies
dotnet restore

# Run
dotnet run
```

You can shut down the web server manually by pressing Ctrl-C.

## Important Snippets

### 1. Add the Cookie and OIDC Middleware

```csharp
// Startup.cs

public void ConfigureServices(IServiceCollection services)
{
    // Add authentication services
    services.AddAuthentication(
        options => options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme);

    // Code omitted for brevity...
}

// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, IOptions<Auth0Settings> auth0Settings)
{
    // Code omitted for brevity...

    // Add the cookie middleware
    app.UseCookieAuthentication(new CookieAuthenticationOptions
    {
        AutomaticAuthenticate = true,
        AutomaticChallenge = true
    });

    // Add the OIDC middleware
    app.UseOpenIdConnectAuthentication(new OpenIdConnectOptions("Auth0")
    {
        // Set the authority to your Auth0 domain
        Authority = $"https://{auth0Settings.Value.Domain}",

        // Configure the Auth0 Client ID and Client Secret
        ClientId = auth0Settings.Value.ClientId,
        ClientSecret = auth0Settings.Value.ClientSecret,

        // Do not automatically authenticate and challenge
        AutomaticAuthenticate = false,
        AutomaticChallenge = false,

        // Set response type to code
        ResponseType = "code",

        // Set the callback path, so Auth0 will call back to http://localhost:5000/signin-auth0 
        // Also ensure that you have added the URL as an Allowed Callback URL in your Auth0 dashboard 
        CallbackPath = new PathString("/signin-auth0"),

        // Configure the Claims Issuer to be Auth0
        ClaimsIssuer = "Auth0"
    });

    app.UseMvc(routes =>
    {
        routes.MapRoute(
            name: "default",
            template: "{controller=Home}/{action=Index}/{id?}");
    });
}
```

### 2. Challenge the OIDC middleware to log the user in

To log the user in, simply challenge the OIDC middleware. This will redirect to Auth0 to authenticate the user.

```csharp
// Controllers/AccountController.cs

public IActionResult Login(string returnUrl = "/")
{
    return new ChallengeResult("Auth0", new AuthenticationProperties() { RedirectUri = returnUrl });
}
```

### 3. Log the user out

To log the user out, call the `SignOutAsync` method for both the OIDC middleware as well as the Cookie middleware.

```
// Controllers/AccountController.cs

[Authorize]
public IActionResult Logout()
{
    HttpContext.Authentication.SignOutAsync("Auth0");
    HttpContext.Authentication.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

    return RedirectToAction("Index", "Home");
}
```