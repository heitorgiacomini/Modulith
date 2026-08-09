FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Bootstrapper/OrderingSubgraph/OrderingSubgraph.csproj", "Bootstrapper/OrderingSubgraph/"]
COPY ["Modules/Ordering/Ordering.csproj", "Modules/Ordering/"]
COPY ["Shared/Shared/Shared.csproj", "Shared/Shared/"]
COPY ["Shared/Shared.Contracts/Shared.Contracts.csproj", "Shared/Shared.Contracts/"]
COPY ["Shared/Shared.Messaging/Shared.Messaging.csproj", "Shared/Shared.Messaging/"]
RUN dotnet restore "./Bootstrapper/OrderingSubgraph/OrderingSubgraph.csproj"
COPY . .
WORKDIR "/src/Bootstrapper/OrderingSubgraph"
RUN dotnet build "./OrderingSubgraph.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./OrderingSubgraph.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "OrderingSubgraph.dll"]
