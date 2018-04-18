#!/usr/bin/env bash
docker build -t auth0-aspnetcore-mvc-00-starter-seed .
docker run -it -p 3000:3000 -e "ASPNETCORE_URLS=http://*:3000" auth0-aspnetcore-mvc-00-starter-seed
