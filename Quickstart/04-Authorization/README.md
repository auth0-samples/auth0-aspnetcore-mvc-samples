# Authorization

This example shows how to allow only users in certain roles to access a particular action.

You can read a quickstart for this sample [here](https://auth0.com/docs/quickstart/webapp/aspnet-core/04-authorization). 

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

### 1. Specify the Roles who can access a particular action

```csharp
// /Controllers/HomeController.cs

[Authorize(Roles = "admin")]
public IActionResult Admin()
{
    return View();
}
```
