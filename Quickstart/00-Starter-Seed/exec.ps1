docker build -t auth0-aspnetcore-v1-mvc-00-starter-seed .
docker run -p 3000:3000 -e "ASPNETCORE_URLS=http://*:3000" auth0-aspnetcore-v1-mvc-00-starter-seed
