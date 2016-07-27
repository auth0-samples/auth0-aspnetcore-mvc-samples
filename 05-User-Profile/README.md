# User Profile

This example shows how to extract user profile information from claims and display the user's profile in your application.

You can read a quickstart for this sample [here](https://auth0.com/docs/quickstart/webapp/aspnet-core/05-user-profile). 

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

### 1. Create a View Model to store the Profile

```csharp
// /ViewModels/UserProfileViewModel.cs

public class UserProfileViewModel
{
    public string EmailAddress { get; set; }

    public string Name { get; set; }

    public string ProfileImage { get; set; }
}
```

### 2. Extract profile from claims

```csharp
// /Controllers/AccountController.cs

[Authorize]
public IActionResult Profile()
{
    return View(new UserProfileViewModel()
    {
        Name = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value,
        EmailAddress = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value,
        ProfileImage = User.Claims.FirstOrDefault(c => c.Type == "picture")?.Value
    });
}
```

### 3. User Profile view

```html
<!-- /Views/Accounts/Profile.cshtml -->

@model SampleMvcApp.ViewModels.UserProfileViewModel
@{
    ViewData["Title"] = "User Profile";
}

<div class="row">
    <div class="col-md-12">
        <div class="row">
            <h2>@ViewData["Title"].</h2>

            <div class="col-md-2">
                <img src="@Model.ProfileImage"
                     alt="" class="img-rounded img-responsive" />
            </div>
            <div class="col-md-4">
                <h3>@Model.Name</h3>
                <p>
                    <i class="glyphicon glyphicon-envelope"></i> @Model.EmailAddress
                </p>
            </div>
        </div>
    </div>
</div>
```