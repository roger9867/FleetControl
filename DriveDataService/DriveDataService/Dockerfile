# syntax=docker/dockerfile:1

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project configuration
COPY TelemetryDataService.csproj .
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet restore TelemetryDataService.csproj

# Copy source code
COPY . .
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet publish TelemetryDataService.csproj -c Release -o /app --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

# Container entry point
ENTRYPOINT ["dotnet", "TelemetryDataService.dll"]
