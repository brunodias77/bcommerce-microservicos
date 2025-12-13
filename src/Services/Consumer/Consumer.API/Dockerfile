FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# Copy csproj files and restore
COPY ["src/Services/Consumer/Consumer.API/Consumer.API.csproj", "src/Services/Consumer/Consumer.API/"]
COPY ["src/Services/Shared/IntegrationEvents/Shared.IntegrationEvents.csproj", "src/Services/Shared/IntegrationEvents/"]
COPY ["src/BuildingBlocks/EventBus/EventBus.Abstractions/EventBus.Abstractions.csproj", "src/BuildingBlocks/EventBus/EventBus.Abstractions/"]
COPY ["src/BuildingBlocks/EventBus/EventBus.RabbitMQ/EventBus.RabbitMQ.csproj", "src/BuildingBlocks/EventBus/EventBus.RabbitMQ/"]

RUN dotnet restore "src/Services/Consumer/Consumer.API/Consumer.API.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/src/Services/Consumer/Consumer.API"
RUN dotnet build "Consumer.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Consumer.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:80/health || exit 1

ENTRYPOINT ["dotnet", "Consumer.API.dll"]
