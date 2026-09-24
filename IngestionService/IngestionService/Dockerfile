# syntax=docker/dockerfile:1

# Build-Stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# csproj zuerst kopieren
COPY IngestionService.csproj .
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet restore IngestionService.csproj

COPY . .
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet publish IngestionService.csproj -c Release -o /app --no-restore

# Runtime-Stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENTRYPOINT ["dotnet", "IngestionService.dll"]
