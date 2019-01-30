# ASP.NET Core OIDC Sample - HS256

This sample demonstrates how you can configure the standard OIDC middleware to authenticate users of an ASP.NET Core MVC application using Auth0 **when using HS256 signed tokens**.

For more information on how to use Auth0 with ASP.NET Core, please look at the [ASP.NET Core Quickstart](https://auth0.com/docs/quickstart/webapp/aspnet-core)

## 1. Configure your Auth0 application

Go to the [Auth0 Dashboard](https://manage.auth0.com) and ensure that you:

* Add the URL `http://localhost:3000/callback` to your list of callback URLs
* Configure your application to sign JWT using HS256 (you find this under Settings > Show Advanced Settings > OAuth > JsonWebToken Signature Algorithm)

## 2. Add the cookie and OIDC NuGet packages

```
Install-Package Microsoft.AspNetCore.Authentication.Cookies
Install-Package Microsoft.AspNetCore.Authentication.OpenIdConnect
```

## 3. Configure Authentication Services

In the `ConfigureServices` of your `Startup` class, ensure that you add the authentication services:

```
public void ConfigureServices(IServiceCollection services)
{
    // Add authentication services
    services.AddAuthentication(
        options => options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme);

    // Add framework services.
    services.AddMvc();

    // Add functionality to inject IOptions<T>
    services.AddOptions();

    // Add the Auth0 Settings object so it can be injected
    services.Configure<Auth0Settings>(Configuration.GetSection("Auth0"));
}
```

## 4. Configure the cookie and OIDC middleware

In the `Configure` method of your `Startup` class, prepare the signature validation key, register the Cookie and OIDC middleware:

```
// Add the cookie middleware
app.UseCookieAuthentication(new CookieAuthenticationOptions
{
    AutomaticAuthenticate = true,
    AutomaticChallenge = true
});

// Get the client secret used for signing the tokens
var keyAsBytes = Encoding.UTF8.GetBytes(auth0Settings.Value.ClientSecret);
var issuerSigningKey = new SymmetricSecurityKey(keyAsBytes);

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

    // Set the callback path, so Auth0 will call back to http://localhost:3000/callback
    // Also ensure that you have added the URL as an Allowed Callback URL in your Auth0 dashboard
    CallbackPath = new PathString("/callback"),

    // Configure the Claims Issuer to be Auth0
    ClaimsIssuer = "Auth0",

    // The UserInfo endpoint does not really return any extra claims which were not returned in the original auth response, so
    // we can save ourselves from making an extra request
    GetClaimsFromUserInfoEndpoint = false,

    // manually setup the signature validation key
    TokenValidationParameters = new TokenValidationParameters
    {
        IssuerSigningKey = issuerSigningKey
    }
});
```

## 5. Handle the login and logout routes

The cookie middleware will redirect users to the `account/login` and `account/logout` paths respectively to log users in or out. We need to add an `AccountController` class with actions to handle these routes:

```
public class AccountController : Controller
{
    public IActionResult Login(string returnUrl = "/")
    {
        return new ChallengeResult("Auth0", new AuthenticationProperties() {RedirectUri = returnUrl});
    }

    [Authorize]
    public async Task Logout()
    {
        await HttpContext.Authentication.SignOutAsync("Auth0", new AuthenticationProperties
        {
            // Indicate here where Auth0 should redirect the user after a logout.
            // Note that the resulting absolute Uri must be whitelisted in the
            // **Allowed Logout URLs** settings for the client.
            RedirectUri = Url.Action("Index", "Home")
        });
        await HttpContext.Authentication.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
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
