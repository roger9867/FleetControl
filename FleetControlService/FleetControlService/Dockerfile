FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY FleetControlServer.Api/FleetControlServer.Api.csproj FleetControlServer.Api/
COPY FleetControlServer.Data/FleetControlServer.Data.csproj FleetControlServer.Data/
COPY FleetControlServer.Domain/FleetControlServer.Domain.csproj FleetControlServer.Domain/
COPY FleetControlServer.Service/FleetControlServer.Service.csproj FleetControlServer.Service/
COPY FleetControlServer.Infra/FleetControlServer.Infra.csproj FleetControlServer.Infra/
RUN dotnet restore FleetControlServer.Api/FleetControlServer.Api.csproj

COPY FleetControlServer.Api/ FleetControlServer.Api/
COPY FleetControlServer.Data/ FleetControlServer.Data/
COPY FleetControlServer.Domain/ FleetControlServer.Domain/
COPY FleetControlServer.Service/ FleetControlServer.Service/
COPY FleetControlServer.Infra/ FleetControlServer.Infra/
RUN dotnet publish FleetControlServer.Api/FleetControlServer.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENV Http__Port=5000
ENV Grpc__Port=5001
EXPOSE 5000
EXPOSE 5001

ENTRYPOINT ["dotnet", "FleetControlServer.Api.dll"]
