# ASP.NET Core OIDC Sample - HS256

This sample demonstrates how you can configure the standard OIDC middleware to authenticate users of an ASP.NET Core MVC application using Auth0 **when using HS256 signed tokens**.

For more information on how to use Auth0 with ASP.NET Core, please look at the [ASP.NET Core Quickstart](https://auth0.com/docs/quickstart/webapp/aspnet-core)

## Requirements

* .[NET Core 3.1 SDK](https://www.microsoft.com/net/download/core)

## To run this project

1. Ensure that you hae configured your Auth0 Client to sign JWT using HS256 (you find this under Settings > Show Advanced Settings > OAuth > JsonWebToken Signature Algorithm)

2. Ensure that you have replaced the [appsettings.json](appsettings.json) file with the values for your Auth0 account.

3. Run the application from the command line:

    ```bash
    dotnet run
    ```

4. Go to `http://localhost:3000` in your web browser to view the website.

## To run this project with docker

In order to run the example with docker you need to have **Docker** installed.

Execute in command line `sh exec.sh` to run the Docker in Linux or macOS, or `.\exec.ps1` to run the Docker in Windows.

## Important Snippets

## 1. Configure Authentication Services

In the `ConfigureServices` of your `Startup` class, prepare the signature validation key and ensure that you add the authentication services:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    // Get the client secret used for signing the tokens
    var keyAsBytes = Encoding.UTF8.GetBytes(Configuration["Auth0:ClientSecret"]);

    // if using non-base64 encoded key, just use:
    //var keyAsBase64 = auth0Settings.Value.ClientSecret.Replace('_', '/').Replace('-', '+');
    //var keyAsBytes = Convert.FromBase64String(keyAsBase64);

    var issuerSigningKey = new SymmetricSecurityKey(keyAsBytes);

    // Add authentication services
    services.AddAuthentication(options => {
        options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
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
        options.Scope.Add("profile");
        options.Scope.Add("email");

        // Set the callback path, so Auth0 will call back to http://localhost:3000/callback
        // Also ensure that you have added the URL as an Allowed Callback URL in your Auth0 dashboard
        options.CallbackPath = new PathString("/callback");

        // Configure the Claims Issuer to be Auth0
        options.ClaimsIssuer = "Auth0";

        // manually setup the signature validation key
        options.TokenValidationParameters = new TokenValidationParameters
        {
            IssuerSigningKey = issuerSigningKey
        };

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

    // Add framework services.
    services.AddControllersWithViews();
}
```

## 2. Configure the Authentication middleware

In the `Configure` method of your `Startup` class call the `UseAuthentication` extension method:

```csharp
public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    if (env.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
        app.UseBrowserLink();
    }
    else
    {
        app.UseExceptionHandler("/Home/Error");
    }

    app.UseStaticFiles();
	app.UseRouting();

    app.UseAuthentication();
	app.UseAuthorization();

    app.UseEndpoints(endpoints =>
    {
        endpoints.MapDefaultControllerRoute();
    });
}

```

## 3. Handle the login and logout routes

The cookie middleware will redirect users to the `account/login` and `account/logout` paths respectively to log users in or out. We need to add an `AccountController` class with actions to handle these routes:

```csharp
public class AccountController : Controller
{
    public async Task Login(string returnUrl = "/")
    {
        await HttpContext.ChallengeAsync("Auth0", new AuthenticationProperties() { RedirectUri = returnUrl });
    }

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
}
```
