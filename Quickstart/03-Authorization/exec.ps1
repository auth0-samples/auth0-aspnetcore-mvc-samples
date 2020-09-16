dotnet dev-certs https -ep $env:USERPROFILE\.aspnet\https\aspnetapp.pfx -p auth0-sample
dotnet dev-certs https --trust
docker build -t auth0-aspnetcore-mvc-03-authorization .
docker run -it -p 44360:443 -v $env:USERPROFILE/.aspnet/https:/https:ro auth0-aspnetcore-mvc-03-authorization
