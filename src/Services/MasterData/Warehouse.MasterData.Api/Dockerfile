# Warehouse MasterData API (Catalog + Partners modules). Owns the "masterdata" database
# and publishes catalog integration events through the transactional outbox.
#
# Build context is the REPOSITORY ROOT (the project references SharedKernel, Contracts,
# ServiceDefaults and its modules across directories, plus Directory.*.props at the root):
#   docker build -t warehouse-masterdata-api -f src/Services/MasterData/Warehouse.MasterData.Api/Dockerfile .

# --- build stage ----------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "src/Services/MasterData/Warehouse.MasterData.Api/Warehouse.MasterData.Api.csproj"
RUN dotnet publish "src/Services/MasterData/Warehouse.MasterData.Api/Warehouse.MasterData.Api.csproj" \
    -c Release -o /app/publish --no-restore /p:UseAppHost=false

# --- runtime stage --------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Warehouse.MasterData.Api.dll"]
