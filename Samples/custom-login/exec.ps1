docker build -t auth0-aspnetcore-v1-mvc-custom-login .
docker run -it -p 3000:3000 -e "ASPNETCORE_URLS=http://*:3000" auth0-aspnetcore-v1-mvc-custom-login