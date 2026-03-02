FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Inventory/src/Warehouse.Inventory.Api/Warehouse.Inventory.Api.csproj", "Inventory/src/Warehouse.Inventory.Api/"]
COPY ["Common/Warehouse.SharedKernel/Warehouse.SharedKernel.csproj", "Common/Warehouse.SharedKernel/"]
COPY ["Inventory/src/Warehouse.Inventory.Domain/Warehouse.Inventory.Domain.csproj", "Inventory/src/Warehouse.Inventory.Domain/"]
COPY ["Inventory/src/Warehouse.Inventory.Infrastructure/Warehouse.Inventory.Infrastructure.csproj", "Inventory/src/Warehouse.Inventory.Infrastructure/"]
COPY ["Common/Warehouse.Contracts/Warehouse.Contracts.csproj", "Common/Warehouse.Contracts/"]
COPY ["Inventory/src/Warehouse.Inventory.Application/Warehouse.Inventory.Application.csproj", "Inventory/src/Warehouse.Inventory.Application/"]
RUN dotnet restore "Inventory/src/Warehouse.Inventory.Api/Warehouse.Inventory.Api.csproj"
COPY . .
WORKDIR "/src/Inventory/src/Warehouse.Inventory.Api"
RUN dotnet build "./Warehouse.Inventory.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Warehouse.Inventory.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Warehouse.Inventory.Api.dll"]
