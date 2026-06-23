# Warehouse API gateway (YARP). Routes /api/* to the three backend services; cluster
# destinations are logical service names resolved at runtime by service discovery.
#
# Build context is the REPOSITORY ROOT (the project references ServiceDefaults across
# directories and relies on Directory.*.props at the root):
#   docker build -t warehouse-gateway -f src/Gateway/Warehouse.Gateway/Dockerfile .

# --- build stage ----------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "src/Gateway/Warehouse.Gateway/Warehouse.Gateway.csproj"
RUN dotnet publish "src/Gateway/Warehouse.Gateway/Warehouse.Gateway.csproj" \
    -c Release -o /app/publish --no-restore /p:UseAppHost=false

# --- runtime stage --------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Warehouse.Gateway.dll"]
