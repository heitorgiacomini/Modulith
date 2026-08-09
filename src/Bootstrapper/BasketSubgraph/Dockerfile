FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Bootstrapper/BasketSubgraph/BasketSubgraph.csproj", "Bootstrapper/BasketSubgraph/"]
COPY ["Modules/Basket/Basket.csproj", "Modules/Basket/"]
COPY ["Modules/Catalog/Catalog.Contracts/Catalog.Contracts.csproj", "Modules/Catalog/Catalog.Contracts/"]
COPY ["Shared/Shared/Shared.csproj", "Shared/Shared/"]
COPY ["Shared/Shared.Contracts/Shared.Contracts.csproj", "Shared/Shared.Contracts/"]
COPY ["Shared/Shared.Messaging/Shared.Messaging.csproj", "Shared/Shared.Messaging/"]
RUN dotnet restore "./Bootstrapper/BasketSubgraph/BasketSubgraph.csproj"
COPY . .
WORKDIR "/src/Bootstrapper/BasketSubgraph"
RUN dotnet build "./BasketSubgraph.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./BasketSubgraph.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BasketSubgraph.dll"]
