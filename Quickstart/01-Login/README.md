# Login

This example shows how to add **_Login/SignUp_** to your application using the hosted version of the `Lock` widget.

You can read a [quickstart for this sample here](https://auth0.com/docs/quickstart/webapp/aspnet-core/01-login).

## Requirements

- [.NET SDK](https://dotnet.microsoft.com/download) (.NET Core 3.1 or .NET 5.0+)

## To run this project

1. Ensure that you have replaced the `appsettings.json` file with the values for your Auth0 account.

2. Run the application from the command line:

```bash
dotnet run
```

3. Go to `http://localhost:3000` in your web browser to view the website.

## To run this project with Docker

In order to run the example with Docker you need to have [Docker](https://docker.com/products/docker-desktop) installed.

To build the Docker image and run the project inside a container, run the following command in a terminal, depending on your operating system:

```
# Mac
sh exec.sh

# Windows (using Powershell)
.\exec.ps1
```

## Important Snippets

### 1. Register the Cookie and OIDC Authentication handlers

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
    .AddOpenIdConnect("Auth0", options => {
        // Set the authority to your Auth0 domain
        options.Authority = $"https://{Configuration["Auth0:Domain"]}";

        // Configure the Auth0 Client ID and Client Secret
        options.ClientId = Configuration["Auth0:ClientId"];
        options.ClientSecret = Configuration["Auth0:ClientSecret"];

        // Set response type to code
        options.ResponseType = "code";

        // Configure the scope
        options.Scope.Clear();
        options.Scope.Add("openid");

        // Set the callback path, so Auth0 will call back to http://localhost:3000/callback
        // Also ensure that you have added the URL as an Allowed Callback URL in your Auth0 dashboard
        options.CallbackPath = new PathString("/callback");

        // Configure the Claims Issuer to be Auth0
        options.ClaimsIssuer = "Auth0";
    });

    // Add framework services.
    services.AddControllersWithViews();
}
```

### 2. Register the Authentication middleware

```csharp
public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
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
	app.UseRouting();
    // Register the Authentication middleware
    app.UseAuthentication();
    app.UseAuthorization();

    app.UseEndpoints(endpoints => {
        endpoints.MapDefaultControllerRoute();
    });
}
```

### 3. Challenge the OIDC middleware to log the user in

To log the user in, simply challenge the OIDC middleware. This will redirect to Auth0 to authenticate the user.

```csharp
// Controllers/AccountController.cs

public IActionResult Login(string returnUrl = "/")
{
    await HttpContext.ChallengeAsync("Auth0", new AuthenticationProperties() { RedirectUri = returnUrl });
}
```

### 4. Log the user out

To log the user out, call the `SignOutAsync` method for both the OIDC middleware as well as the Cookie middleware.

```csharp
// Controllers/AccountController.cs

[Authorize]
public async Task Logout()
{
    await HttpContext.SignOutAsync("Auth0", new AuthenticationProperties
    {
        // Indicate here where Auth0 should redirect the user after a logout.
        // Note that the resulting absolute Uri must be whitelisted in the
        // **Allowed Logout URLs** settings for the client.
        RedirectUri = Url.Action("Index", "Home")
    });
    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
}
```

When configuring the OIDC middleware, you will have to handle the `OnRedirectToIdentityProviderForSignOut` event to redirect
the user to the [Auth0 logout endpoint](https://auth0.com/docs/logout#log-out-a-user):

```csharp
services.AddAuthentication(options => {
    // Code omitted for brevity
})
.AddCookie()
.AddOpenIdConnect("Auth0", options => {
    // Code omitted for brevity

    options.Events = new OpenIdConnectEvents
    {
        // handle the logout redirection
        OnRedirectToIdentityProviderForSignOut = (context) =>
        {
            var logoutUri = $"https://{Configuration["Auth0:Domain"]}/v2/logout?client_id={Configuration["Auth0:ClientId"]}";

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
        }
    };
});
```

## Passing additional parameters to the /authorize endpoint

When asking Auth0 to authenticate a user, you might want to provide additional parameters to the `/authorize` endpoint, such as the `connection`, `offline_access`, `audience` or others. In order to do so, you need to handle the `OnRedirectToIdentityProvider` event when configuring the `OpenIdConnectionOptions` and call the `ProtocolMessage.SetParameter` method on the supplied `RedirectContext`:

```csharp
// Add the OIDC middleware
services.AddAuthentication(options => {
    // Code omitted for brevity
})
.AddCookie()
.AddOpenIdConnect("Auth0", options => {
    // Code omitted for brevity

    options.Events = new OpenIdConnectEvents
    {
        OnRedirectToIdentityProvider = context =>
        {
            // add any custom parameters here
            context.ProtocolMessage.SetParameter("connection", "google-oauth2");

            return Task.CompletedTask;
        }
    };
});
```

If you need to make this dynamic (i.e. provide information that affects what parameters will be set), take a look at [this blog post](http://www.jerriepelser.com/blog/adding-parameters-to-openid-connect-authorization-url/).
