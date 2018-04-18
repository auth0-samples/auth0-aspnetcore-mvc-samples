docker build -t auth0-aspnetcore-mvc-00-starter-seed .
docker run -p 3010:3010 -e "ASPNETCORE_URLS=http://*:3010" -it auth0-aspnetcore-mvc-00-starter-seed
