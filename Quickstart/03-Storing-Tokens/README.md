# Storing Tokens

This example shows how to save the `access_token` and `id_token` as claims.

You can read a quickstart for this sample [here](https://auth0.com/docs/quickstart/webapp/aspnet-core/04-storing-tokens). 

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

### 1. Save the tokens

Be sure to set the `SaveTokens` property to `true`, and then add the tokens as claims in the `OnTicketReceived` event.

```csharp
// Startup.cs

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
    ClaimsIssuer = "Auth0",

    // Saves tokens to the AuthenticationProperties
    SaveTokens = true,

    Events = new OpenIdConnectEvents()
    {
        OnTicketReceived = context =>
        {
            // Get the ClaimsIdentity
            var identity = context.Principal.Identity as ClaimsIdentity;
            if (identity != null)
            {
                // Check if token names are stored in Properties
                if (context.Properties.Items.ContainsKey(".TokenNames"))
                {
                    // Token names a semicolon separated
                    string[] tokenNames = context.Properties.Items[".TokenNames"].Split(';');

                    // Add each token value as Claim
                    foreach (var tokenName in tokenNames)
                    {
                        // Tokens are stored in a Dictionary with the Key ".Token.<token name>"
                        string tokenValue = context.Properties.Items[$".Token.{tokenName}"];

                        identity.AddClaim(new Claim(tokenName, tokenValue));
                    }
                }
            }

            return Task.CompletedTask;
        },
        // ...
    }
});

```