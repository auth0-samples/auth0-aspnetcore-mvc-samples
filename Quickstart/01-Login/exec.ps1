docker build -t auth0-aspnetcore-v1-mvc-01-login .
docker run -p 3000:3000 -e "ASPNETCORE_URLS=http://*:3000" auth0-aspnetcore-v1-mvc-01-login
