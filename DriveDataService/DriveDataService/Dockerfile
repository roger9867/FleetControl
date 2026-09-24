# syntax=docker/dockerfile:1

# Build-Stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Projektkonfiguration kopieren
COPY TelemetryDataService.csproj .
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet restore TelemetryDataService.csproj

# Quellcode kopieren
COPY . .
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet publish TelemetryDataService.csproj -c Release -o /app --no-restore

# Runtime-Stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

# Einstiegspunkt des Containers
ENTRYPOINT ["dotnet", "TelemetryDataService.dll"]
