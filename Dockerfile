FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore src/PedagoraPilot.Api/PedagoraPilot.Api.csproj
RUN dotnet publish src/PedagoraPilot.Api/PedagoraPilot.Api.csproj -c Release --no-restore -o /publish /p:UseAppHost=false
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
COPY --from=build /publish .
ENV ASPNETCORE_URLS=http://+:8080 ASPNETCORE_ENVIRONMENT=Staging
EXPOSE 8080
HEALTHCHECK --interval=20s --timeout=5s --start-period=100s --retries=6 CMD curl -fsS http://127.0.0.1:8080/api/system/ready >/dev/null || exit 1
ENTRYPOINT ["dotnet", "PedagoraPilot.Api.dll"]
