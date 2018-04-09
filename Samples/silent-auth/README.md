# Silent authentication

This example shows how to attempt a [silent authentication](https://auth0.com/docs/api-auth/tutorials/silent-authentication) 
first, and fallback to a regular login if the silent authentication failed.

If you have multiple applications that share the same identity provider (Auth0) and
want to implement an "automatic Single Sign-On", where the login screen is not shown
if there is already a session at the identity provider, you can use this mechanism.

## Requirements

* .[NET Core 2.0 SDK](https://www.microsoft.com/net/download/core)

## To run this project

1. Ensure that you have replaced the [appsettings.json](appsettings.json) file with the values for your Auth0 account.

2. Run the application from the command line:

    ```bash
    dotnet run
    ```

3. Go to `http://localhost:5000` in your web browser to view the website.

## To run this project with docker

In order to run the example with docker you need to have **Docker** installed.

Execute in command line `sh exec.sh` to run the Docker in Linux or macOS, or `.\exec.ps1` to run the Docker in Windows.

## Important Snippets

### 1. Add the prompt=none parameter

Silent authentication is triggered by adding the `prompt=none` parameter when
requesting an authentication. It's up to you to decide to always try silent 
authentication by default, or do it only if specifically requested.

In this sample, we are attempting silent authentication by default, so we handle the
`OnRedirectToIdentityProvider` event in the `OpenIDConnectOptions` to add the parameter.
Notice that we omit adding the `prompt=none` if the `loginrequired` custom property is present, this is explained later.

```csharp
// Startup.cs

services.AddAuthentication(options => {
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddCookie()
.AddOpenIdConnect("Auth0", options => {
    // other options omitted for brevity
    // [...] 

    options.Events = new OpenIdConnectEvents
    {
        // other events, like logout, omitted for brevity
        // [...]
        OnRedirectToIdentityProvider = (context) =>
        {
            if (!context.Properties.Items.ContainsKey("loginrequired"))
            {
                context.ProtocolMessage.Parameters.Add("prompt", "none");
            }
            return Task.CompletedTask;
        }
    };   
});
```

### 2. Handle silent authentication error

Silent authentication can fail for a number of reasons, for instance if the user doesn't have a valid session at the identity provider, or needs to give consent, or needs to be redirected to another please. 
For each of those cases, Auth0 will return a specific error to the callback URL. We will
check for those in the `OnMessageReceived` event and, if found, trigger a new
authentication request, this time signaling that a login is required by adding the `loginrequired` custom property (so that the code above doesn't add the `prompt=none` parameter).

```csharp
// Startup.cs

services.AddAuthentication(options => {
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddCookie()
.AddOpenIdConnect("Auth0", options => {
    // other options omitted for brevity
    // [...] 

    options.Events = new OpenIdConnectEvents
    {
        // other events, like logout, omitted for brevity
        // [...]
        OnRedirectToIdentityProvider = (context) =>
        {
            // omitted for brevity
            // [...]
        },
        OnMessageReceived = async (context) =>
        {
            string[] LoginRequiredErrors = 
                { "login_required", "consent_required", "interaction_required" };
            string error;
            context.ProtocolMessage.Parameters.TryGetValue("error", out error);
            if (LoginRequiredErrors.Contains(error))
            {
                var authenticationProperties = new Microsoft.AspNetCore.Http.Authentication.AuthenticationProperties()
                {
                    RedirectUri = context.Properties.RedirectUri
                };
                authenticationProperties.Items["loginrequired"] = "true";
                await context.HttpContext.Authentication.ChallengeAsync("Auth0", authenticationProperties);
                context.HandleResponse();
            }
        }
    };   
});
```
