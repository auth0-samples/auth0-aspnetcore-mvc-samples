# Storing Tokens

This example shows how to save the `access_token` and `id_token` as claims.

You can read a quickstart for this sample [here](https://auth0.com/docs/quickstart/webapp/aspnet-core/02-storing-tokens). 

## Requirements

* .[NET Core 2.0 SDK](https://www.microsoft.com/net/download/core)

## To run this project

1. Ensure that you have replaced the [appsettings.json](SampleMvcApp/appsettings.json) file with the values for your Auth0 account.

2. Run the application from the command line:

    ```bash
    dotnet run
    ```

3. Go to `http://localhost:5000` in your web browser to view the website.

## To run this project with docker

In order to run the example with docker you need to have **Docker** installed.

Execute in command line `sh exec.sh` to run the Docker in Linux or macOS, or `.\exec.ps1` to run the Docker in Windows.

## Important Snippets

### 1. Save the tokens

Be sure to set the `SaveTokens` property to `true`.

```csharp
// Startup.cs

// Add authentication services
services.AddAuthentication(options => {
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie()
.AddOpenIdConnect("Auth0", options => {
    // other settings omitted for brevity

    // Saves tokens to the AuthenticationProperties
    options.SaveTokens = true;
});
```

### 2. Access tokens

You can access the tokens from inside any controller action by calling the `GetTokenAsync` extension method, e.g.:

```csharp
public class HomeController : Controller
{
    public async Task<IActionResult> Index()
    {
        // If the user is authenticated, then this is how you can get the access_token and id_token
        if (User.Identity.IsAuthenticated)
        {
            string accessToken = await HttpContext.GetTokenAsync("access_token");
            string idToken = await HttpContext.GetTokenAsync("id_token");

            // Now you can use them. For more info on when and how to use the 
            // access_token and id_token, see https://auth0.com/docs/tokens
        }

        return View();
    }

    public IActionResult Error()
    {
        return View();
    }
}
```
