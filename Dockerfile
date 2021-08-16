FROM mcr.microsoft.com/dotnet/aspnet:5.0

WORKDIR /app

COPY out/* ./
COPY config.json ./

ENTRYPOINT ["dotnet", "r20esdiscordbot2.dll"]
