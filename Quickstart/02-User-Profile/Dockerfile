FROM microsoft/dotnet:2.1-sdk

WORKDIR /source

COPY SampleMvcApp/*.csproj .
RUN ["dotnet", "restore"]

COPY SampleMvcApp/. .
RUN ["dotnet", "build"]

EXPOSE 3000/tcp

ENTRYPOINT ["dotnet", "run"]
