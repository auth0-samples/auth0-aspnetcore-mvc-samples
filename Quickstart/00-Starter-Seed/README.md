# Seed Project for the ASP.NET Core MVC Quickstart

This seed project can be used if you want to follow along with the steps in the [ASP.NET Core MVC Quickstart](https://auth0.com/docs/quickstart/webapp/aspnet-core).

This starter seed is a basic web application which was created using the Yeoman generator for ASP.NET, and also includes some of the dependencies required to use the Cookie and OpenID Connect (OIDC) middleware.

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
