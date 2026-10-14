# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project configurations
COPY FleetControlApi.Api/FleetControlApi.Api.csproj FleetControlApi.Api/
COPY FleetControlApi.Persistence/FleetControlApi.Persistence.csproj FleetControlApi.Persistence/
COPY FleetControlApi.Domain/FleetControlApi.Domain.csproj FleetControlApi.Domain/
COPY FleetControlApi.Service/FleetControlApi.Service.csproj FleetControlApi.Service/
COPY FleetControlApi.Gateway/FleetControlApi.Gateway.csproj FleetControlApi.Gateway/
RUN dotnet restore FleetControlApi.Api/FleetControlApi.Api.csproj

# Copy source code
COPY FleetControlApi.Api/ FleetControlApi.Api/
COPY FleetControlApi.Persistence/ FleetControlApi.Persistence/
COPY FleetControlApi.Domain/ FleetControlApi.Domain/
COPY FleetControlApi.Service/ FleetControlApi.Service/
COPY FleetControlApi.Gateway/ FleetControlApi.Gateway/
RUN dotnet publish FleetControlApi.Api/FleetControlApi.Api.csproj -c Release -o /app

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

# Expose ports
ENV Http__Port=5000
ENV Grpc__Port=5001
EXPOSE 5000
EXPOSE 5001

# Container entry point
ENTRYPOINT ["dotnet", "FleetControlApi.Api.dll"]
