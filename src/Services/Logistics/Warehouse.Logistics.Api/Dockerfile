# Warehouse Logistics API (Deliveries, Orders, PickLists). Owns the "logistics" database
# and consumes/publishes integration events via the transactional outbox.
#
# Build context is the REPOSITORY ROOT (the project references SharedKernel, Contracts,
# ServiceDefaults and its core module across directories, plus Directory.*.props at the root):
#   docker build -t warehouse-logistics-api -f src/Services/Logistics/Warehouse.Logistics.Api/Dockerfile .

# --- build stage ----------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "src/Services/Logistics/Warehouse.Logistics.Api/Warehouse.Logistics.Api.csproj"
RUN dotnet publish "src/Services/Logistics/Warehouse.Logistics.Api/Warehouse.Logistics.Api.csproj" \
    -c Release -o /app/publish --no-restore /p:UseAppHost=false

# --- runtime stage --------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Warehouse.Logistics.Api.dll"]
