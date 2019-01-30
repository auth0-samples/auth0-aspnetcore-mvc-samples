# Custom Login

This example shows how to add ***Login*** to your application using a custom Login screen and using the [Auth0.NET SDK](https://github.com/auth0/auth0.net) to log the user in.

## Getting Started

To run this quickstart you can fork and clone this repo.

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

### 1. The LoginViewModel

The `LoginViewModel` is used in the Login view to capture the user's email address and password.

```csharp
// /ViewModels/LoginViewModel.cs

public class LoginViewModel
{
    [Required]
    [EmailAddress]
    [Display(Name = "Email Address")]
    public string EmailAddress { get; set; }

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; }
}
```

### 2. The Login View

```html
<!-- /Views/Account/Login.cshtml -->

@model SampleMvcApp.ViewModels.LoginViewModel
@{
    ViewData["Title"] = "Log In";
}

<div class="row">
    <div class="col-md-4 col-md-offset-4">
        <section>
            <form asp-controller="Account" asp-action="Login" asp-route-returnurl="@ViewData["ReturnUrl"]" method="post">
                <h4>Log In</h4>
                <hr />
                <div asp-validation-summary="All" class="text-danger"></div>
                <div class="form-group">
                    <label asp-for="EmailAddress"></label>
                    <input asp-for="EmailAddress" class="form-control input-lg" />
                    <span asp-validation-for="EmailAddress" class="text-danger"></span>
                </div>
                <div class="form-group">
                    <label asp-for="Password"></label>
                    <input asp-for="Password" class="form-control input-lg" />
                    <span asp-validation-for="Password" class="text-danger"></span>
                </div>

                <div class="form-group">
                    <button type="submit" class="btn btn-success btn-lg btn-block">Log in</button>
                </div>
                <hr />
                <center><h4>OR</h4></center>
                <div class="form-group">
                    <a class="btn btn-lg btn-default btn-block" asp-controller="Account" asp-action="LoginExternal" asp-route-connection="google-oauth2" asp-route-returnurl="@ViewData["ReturnUrl"]">
                        Login with Google
                    </a>
                </div>
            </form>
        </section>
    </div>
</div>
```

### 3. Log the user in using Auth.NET

```csharp
// /Controllers/AccountController.cs

[HttpPost]
public async Task<IActionResult> Login(LoginViewModel vm, string returnUrl = null)
{
    if (ModelState.IsValid)
    {
        try
        {
            AuthenticationApiClient client = new AuthenticationApiClient(new Uri($"https://{_auth0Settings.Domain}/"));

            var result = await client.AuthenticateAsync(new AuthenticationRequest
            {
                ClientId = _auth0Settings.ClientId,
                Scope = "openid",
                Connection = "Database-Connection", // Specify the correct name of your DB connection
                Username = vm.EmailAddress,
                Password = vm.Password
            });

            // Get user info from token
            var user = await client.GetTokenInfoAsync(result.IdToken);

            // Create claims principal
            var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId),
                new Claim(ClaimTypes.Name, user.FullName)

            }, CookieAuthenticationDefaults.AuthenticationScheme));

            // Sign user into cookie middleware
            await HttpContext.Authentication.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimsPrincipal);

            return RedirectToLocal(returnUrl);
        }
        catch (Exception e)
        {
            ModelState.AddModelError("", e.Message);
        }
    }

    return View(vm);
}
```