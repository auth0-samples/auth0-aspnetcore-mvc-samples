# Login with Embedded Lock

This example shows how to add ***Login/SignUp*** to your application using the embedded version of the `Lock` widget.

> **Please note** that Auth0 recommends that you use the hosted version of Lock, rather than the embedded version.

## Background

When using the normal OIDC middleware, when a user wants to log in and the middleware is called, the user will be redirected to the Auth0 website to sign in using the hosted version of Lock. This may not be the user experience you are looking for. You may for example want to embed Lock inside your application so it has more of the look-and-feel of your own application. In this instance, you can use both Lock and the OIDC middleware together, but it requires a bit of extra work on your side.

Normally when the OIDC middleware initiates the 1st leg of the authentication, it will send along information contained in `state` and `nonce` parameters. After the user has authenticated and Auth0 redirects back to the redirect URL inside your application, in will pass back this `state` and `nonce` parameters. The OIDC middleware is going to pick up that callback to the redirect URL because it will need to exchange the `code` for an `access_token`. It will, however, validate the `state` and `nonce` parameters to protect against CSRF.

This poses a problem. When you embed Lock in your application, the OIDC middleware is not initiating the 1st leg of the OAuth flow. Instead, the embedded Lock widget is initiating that first step.

You will therefore need to construct correct `state` and `nonce` parameters (as if the OIDC middleware did it so that it can validate it correctly), and then be sure to specify the `state` and `nonce` parameters on Lock so that Auth0 can send back the correct values for these parameters after the user has authenticated.

## Getting Started

To run this quickstart you can fork and clone this repo.

Ensure that you have configured the **JsonWebToken Signature Algorithm** for your Auth0 Client to use **RS256** (Go to your Client Settings > Show Advanced Settings > OAuth).

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