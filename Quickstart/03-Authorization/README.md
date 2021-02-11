# Authorization

This example shows how to allow only users in certain roles to access a particular action.

You can read a quickstart for this sample [here](https://auth0.com/docs/quickstart/webapp/aspnet-core/03-authorization).

## Requirements

- [.NET SDK](https://dotnet.microsoft.com/download) (.Net Core 3.1 or .Net 5.0+)

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

### 1. Specify the Roles who can access a particular action

```csharp
// /Controllers/HomeController.cs

[Authorize(Roles = "admin")]
public IActionResult Admin()
{
    return View();
}
```
