#!/usr/bin/env bash
docker build -t auth0-aspnetcore-mvc-custom-login .
docker run -it -p 3000:80 auth0-aspnetcore-mvc-custom-login