# User Profile

This example shows how to extract user profile information from claims and display the user's profile in your application.

You can read a quickstart for this sample [here](https://auth0.com/docs/quickstart/webapp/aspnet-core/02-user-profile).

## Requirements

- [.NET SDK](https://dotnet.microsoft.com/download) (.NET Core 3.1 or .NET 5.0+)

## To run this project

1. Ensure that you have replaced the `appsettings.json` file with the values for your Auth0 account.

2. Run the application from the command line:

```bash
dotnet run
```

3. Go to `http://localhost:3000` in your web browser to view the website.

## Run this project with Docker

In order to run the example with Docker you need to have [Docker](https://docker.com/products/docker-desktop) installed.

To build the Docker image and run the project inside a container, run the following command in a terminal, depending on your operating system:

```
# Mac
sh exec.sh

# Windows (using Powershell)
.\exec.ps1
```

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

@model SampleMvcApp.ViewModels.UserProfileViewModel @{ ViewData["Title"] = "User
Profile"; }

<div class="row">
  <div class="col-md-12">
    <div class="row">
      <h2>@ViewData["Title"].</h2>

      <div class="col-md-2">
        <img
          src="@Model.ProfileImage"
          alt=""
          class="img-rounded img-responsive"
        />
      </div>
      <div class="col-md-4">
        <h3>@Model.Name</h3>
        <p><i class="glyphicon glyphicon-envelope"></i> @Model.EmailAddress</p>
      </div>
    </div>
  </div>
</div>
```
