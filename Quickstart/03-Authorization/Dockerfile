FROM microsoft/aspnetcore-build:1.1

WORKDIR /source

COPY SampleMvcApp/*.csproj .
RUN ["dotnet", "restore"]

COPY SampleMvcApp/. .
RUN ["dotnet", "build"]

EXPOSE 3000/tcp

ENTRYPOINT ["dotnet", "run"]
