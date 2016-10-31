# Login with Embedded Lock

This example shows how to add ***Login/SignUp*** to your application using the embedded version of the `Lock` widget.

You can read a quickstart for this sample [here](https://auth0.com/docs/quickstart/webapp/aspnet-core/02-login-embedded-lock). 

Embedding Lock in your application contains some extra steps as the OIDC middleware needs to be "tricked" into thinking that it initiate the first leg of the authentication. Please be sure to read the full quickstart linked above for a detailed explanation. 

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

### 1. Add the Authentication services and register the OIDC Options with the DI

```csharp
// Startup.cs

public void ConfigureServices(IServiceCollection services)
{
    // Add authentication services
    services.AddAuthentication(
        options => options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme);

    // Configure OIDC
    services.Configure<OpenIdConnectOptions>(options =>
    {
        // Specify Authentication Scheme
        options.AuthenticationScheme = "Auth0";

        // Set the authority to your Auth0 domain
        options.Authority = $"https://{Configuration["auth0:domain"]}";

        // Configure the Auth0 Client ID and Client Secret
        options.ClientId = Configuration["auth0:clientId"];
        options.ClientSecret = Configuration["auth0:clientSecret"];

        // Do not automatically authenticate and challenge
        options.AutomaticAuthenticate = false;
        options.AutomaticChallenge = false;

        // Set response type to code
        options.ResponseType = "code";

        // Set the callback path, so Auth0 will call back to http://localhost:5000/signin-auth0 
        // Also ensure that you have added the URL as an Allowed Callback URL in your Auth0 dashboard 
        options.CallbackPath = new PathString("/signin-auth0");

        // Configure the Claims Issuer to be Auth0
        options.ClaimsIssuer = "Auth0";
    });

    // Code omitted for brevity...
}
```

### 2. Register the Cookie and OIDC Middleware

```csharp
// Startup.cs

public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, IOptions<OpenIdConnectOptions> oidcOptions)
{
    // Omitted for brevity...

    // Add the cookie middleware
    app.UseCookieAuthentication(new CookieAuthenticationOptions
    {
        AutomaticAuthenticate = true,
        AutomaticChallenge = true
    });

    // Add the OIDC middleware
    app.UseOpenIdConnectAuthentication(oidcOptions.Value);

    app.UseMvc(routes =>
    {
        routes.MapRoute(
            name: "default",
            template: "{controller=Home}/{action=Index}/{id?}");
    });
}
```

### 3. Challenge the OIDC middleware to log the user in

To log the user in generate the Lock context (which also sets the correct cookies for the OIDC middleware) using the included helper classes, and redirect to the Login View.

```csharp
// Controllers/AccountController.cs

public IActionResult Login(string returnUrl = "/")
{
    var lockContext = HttpContext.GenerateLockContext(_options.Value, returnUrl);

    return View(lockContext);
}
```
### 4. Display Lock

```html
<!-- /Views/Account/Login.cshtml -->

@model LockContext

<div id="root" style="width: 320px; margin: 40px auto; padding: 10px; border-style: dashed; border-width: 1px;">
    embeded area
</div>
<script src="https://cdn.auth0.com/js/lock/10.4/lock.min.js"></script>
<script>

  var lock = new Auth0Lock('@Model.ClientId', '@Model.Domain', {
    container: 'root',
    auth: {
      redirectUrl: '@Model.CallbackUrl',
      responseType: 'code',
      params: {
        scope: 'openid',
        state: '@Model.State' ,
        nonce: '@Model.Nonce'
      }
    }
  });

  lock.show();
</script>
```

### 5. Log the user out

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