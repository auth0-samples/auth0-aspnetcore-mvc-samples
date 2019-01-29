FROM microsoft/aspnetcore-build:1.1

WORKDIR /source

COPY *.csproj .
RUN ["dotnet", "restore"]

COPY . .
RUN ["dotnet", "build"]

EXPOSE 3000/tcp

ENTRYPOINT ["dotnet", "run"]
