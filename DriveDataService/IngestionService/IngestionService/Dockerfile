FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY IngestionService.csproj .
RUN dotnet restore IngestionService.csproj

COPY . .
RUN dotnet publish IngestionService.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENTRYPOINT ["dotnet", "IngestionService.dll"]
