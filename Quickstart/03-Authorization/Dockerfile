FROM microsoft/aspnetcore-build

WORKDIR /source

COPY SampleMvcApp/*.csproj .
RUN ["dotnet", "restore"]

COPY SampleMvcApp/. .
RUN ["dotnet", "build"]

EXPOSE 3000/tcp

ENTRYPOINT ["dotnet", "run"]
