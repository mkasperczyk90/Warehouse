FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Product/src/Warehouse.Product.Api/Warehouse.Product.Api.csproj", "Product/src/Warehouse.Product.Api/"]
COPY ["Common/Warehouse.SharedKernel/Warehouse.SharedKernel.csproj", "Common/Warehouse.SharedKernel/"]
COPY ["Product/src/Warehouse.Product.Application/Warehouse.Product.Application.csproj", "Product/src/Warehouse.Product.Application/"]
COPY ["Product/src/Warehouse.Product.Domain/Warehouse.Product.Domain.csproj", "Product/src/Warehouse.Product.Domain/"]
COPY ["Common/Warehouse.Contracts/Warehouse.Contracts.csproj", "Common/Warehouse.Contracts/"]
COPY ["Product/src/Warehouse.Product.Infrastructure/Warehouse.Product.Infrastructure.csproj", "Product/src/Warehouse.Product.Infrastructure/"]
RUN dotnet restore "Product/src/Warehouse.Product.Api/Warehouse.Product.Api.csproj"
COPY . .
WORKDIR "/src/Product/src/Warehouse.Product.Api"
RUN dotnet build "./Warehouse.Product.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Warehouse.Product.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Warehouse.Product.Api.dll"]
