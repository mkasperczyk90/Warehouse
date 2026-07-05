# Warehouse CDC Bridge — consumes Debezium's raw CDC JSON off RabbitMQ (docs/BI_Plan.md — CDC
# section) and lands it under bronze-cdc/ in the BI lake S3 bucket. No HTTP surface: a worker, not
# an API.
#
# Build context is the REPOSITORY ROOT (references ServiceDefaults + Directory.*.props at the root):
#   docker build -t warehouse-cdc-bridge -f src/CdcBridge/Warehouse.CdcBridge/Dockerfile .

# --- build stage ----------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
ARG VERSION=0.1.0
ARG GIT_SHA=""
COPY . .
RUN dotnet restore "src/CdcBridge/Warehouse.CdcBridge/Warehouse.CdcBridge.csproj"
RUN dotnet publish "src/CdcBridge/Warehouse.CdcBridge/Warehouse.CdcBridge.csproj" \
    -c Release -o /app/publish --no-restore /p:UseAppHost=false \
    /p:Version=${VERSION} /p:SourceRevisionId=${GIT_SHA}

# --- runtime stage --------------------------------------------------------------
# aspnet (not the smaller `runtime` image): Warehouse.ServiceDefaults references the ASP.NET Core
# shared framework for its OpenTelemetry/health-check wiring even though this worker exposes no HTTP.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Warehouse.CdcBridge.dll"]
