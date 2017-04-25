# ASP.NET Core OAuth2 Sample

This sample demonstrates how you can configure the standard OAuth2 middleware to authenticate users of an ASP.NET Core MVC application using Auth0.

> This sample assumes that you are not using other authentication middleware such as ASP.NET Identity

## 1. Configure your Auth0 application

Go to the Auth0 Dashboard and esure that you add the URL http://localhost:5000/signin-auth0 to your list of callback URLs

## 2. Add the cookie and OAuth NuGet packages

```
Install-Package Microsoft.AspNetCore.Authentication.Cookies
Install-Package Microsoft.AspNetCore.Authentication.OAuth
```

## 3. Configure Authentication Services

In the ConfigureServices of your Startup class, ensure that you add the authentication services:

```
public void ConfigureServices(IServiceCollection services)
{
    // Add authentication services
    services.AddAuthentication(
        options => options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme);

    // Add framework services.
    services.AddMvc();
}  
```

## 4. Configure the cookie and OAuth middleware

In the Configure method of your Startup class, register the Cookie and OAuth middleware:

```
// Add the cookie middleware
app.UseCookieAuthentication(new CookieAuthenticationOptions
{
    AutomaticAuthenticate = true,
    AutomaticChallenge = true,
    LoginPath = new PathString("/login"),
    LogoutPath = new PathString("/logout")
});

// Add the OAuth2 middleware
app.UseOAuthAuthentication(new OAuthOptions
{
    // We need to specify an Authentication Scheme
    AuthenticationScheme = "Auth0",

    // Configure the Auth0 Client ID and Client Secret
    ClientId = Configuration["auth0:clientId"],
    ClientSecret = Configuration["auth0:clientSecret"],

    // Set the callback path, so Auth0 will call back to http://localhost:5000/signin-auth0 
    // Also ensure that you have added the URL as an Allowed Callback URL in your Auth0 dashboard 
    CallbackPath = new PathString("/signin-auth0"),

    // Configure the Auth0 endpoints                
    AuthorizationEndpoint = $"https://{Configuration["auth0:domain"]}/authorize",
    TokenEndpoint = $"https://{Configuration["auth0:domain"]}/oauth/token",
    UserInformationEndpoint = $"https://{Configuration["auth0:domain"]}/userinfo",

    // Set scope to openid. See https://auth0.com/docs/scopes
    Scope = { "openid" },
    
    Events = new OAuthEvents
    {
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

In step 4 we configured the cookie middleware to redirect users to the /login and /logout paths respectively to log users in or out. We need to write handlers for these routes in the Configure method:

```
// Listen for requests on the /login path, and issue a challenge to log in with the OAuth middleware
app.Map("/login", builder =>
{
    builder.Run(async context =>
    {
        // Return a challenge to invoke the Auth0 authentication scheme
        await context.Authentication.ChallengeAsync("Auth0", new AuthenticationProperties() { RedirectUri = "/" });
    });
});

// Listen for requests on the /logout path, and sign the user out
app.Map("/logout", builder =>
{
    builder.Run(async context =>
    {
        // Sign the user out of the authentication middleware (i.e. it will clear the Auth cookie)
        await context.Authentication.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        // Redirect the user to the home page after signing out
        context.Response.Redirect("/");
    });
});
```

You can also alternatively set the AutomaticAuthenticate and AutomaticChallenge of the OAuth middleware to true. If you do this then the OAuth middleware will automatically be invoked when a user tries to access a protected resource.
