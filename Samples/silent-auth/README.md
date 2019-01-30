# Silent authentication

This example shows how to attempt a [silent authentication](https://auth0.com/docs/api-auth/tutorials/silent-authentication)
first, and fallback to a regular login if the silent authentication failed.

If you have multiple applications that share the same identity provider (Auth0) and
want to implement an "automatic Single Sign-On", where the login screen is not shown
if there is already a session at the identity provider, you can use this mechanism.

## Getting Started

To run this sample you can fork and clone this repo.

Be sure to update the `appsettings.json` with your Auth0 settings:

```json
{
  "Auth0": {
    "Domain": "Your Auth0 domain",
    "ClientId": "Your Auth0 Client Id",
    "ClientSecret": "Your Auth0 Client Secret",
    "CallbackUrl": "http://localhost:3000/callback"
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

### 1. Add the prompt=none parameter

Silent authentication is triggered by adding the `prompt=none` parameter when
requesting an authentication. It's up to you to decide to always try silent
authentication by default, or do it only if specifically requested.

In this sample, we are attempting silent authentication by default, so we handle the
`OnRedirectToIdentityProvider` event in the `OpenIDConnectOptions` to add the parameter.
Notice that we omit adding the `prompt=none` if the `loginrequired` custom property is present, this is explained later.

```csharp
// Startup.cs

var options = new OpenIdConnectOptions("Auth0")
{
    [...] other options omitted for brevity

    Events = new OpenIdConnectEvents
    {
        // other events, like logout, omitted for brevity
        // [...]
        OnRedirectToIdentityProvider = (context) =>
        {
            // unless told otherwise, use silent authentication by default
            if (!context.Properties.Items.ContainsKey("loginrequired"))
            {
                context.ProtocolMessage.Parameters.Add("prompt", "none");
            }
            return Task.CompletedTask;
        },
    }
}
```

### 2. Handle silent authentication error

Silent authentication can fail for a number of reasons, for instance if the user doesn't have a valid session at the identity provider, or needs to give consent, or needs to be redirected to another please.
For each of those cases, Auth0 will return a specific error to the callback URL. We will
check for those in the `OnMessageReceived` event and, if found, trigger a new
authentication request, this time signaling that a login is required by adding the `loginrequired` custom property (so that the code above doesn't add the `prompt=none` parameter).

```csharp
var options = new OpenIdConnectOptions("Auth0")
{
    [...] other options omitted for brevity

    Events = new OpenIdConnectEvents
    {
        // other events, like logout, omitted for brevity
        // [...]
        OnRedirectToIdentityProvider = (context) =>
        {
            [...]
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
    }
}
```
