# Seed Project for the ASP.NET Core MVC Quickstart

This seed project can be used if you want to follow along with the steps in the [ASP.NET Core MVC Quickstart](https://auth0.com/docs/quickstart/webapp/aspnet-core).

This starter seed is a basic web application which was created using the Yeoman generator for ASP.NET, and also includes some of the dependencies required to use the Cookie and OpenID Connect (OIDC) middleware.

## Requirements

* .[NET Core 3.1 SDK](https://www.microsoft.com/net/download/core)

## To run this project

1. Ensure that you have replaced the [appsettings.json](SampleMvcApp/appsettings.json) file with the values for your Auth0 account.

2. Run the application from the command line:

    ```bash
    dotnet run
    ```

3. Go to `http://localhost:3000` in your web browser to view the website.

## To run this project with docker

In order to run the example with docker you need to have **Docker** installed.

Execute in command line `sh exec.sh` to run the Docker in Linux or macOS, or `.\exec.ps1` to run the Docker in Windows.
