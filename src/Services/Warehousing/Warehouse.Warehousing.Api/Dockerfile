# Warehouse Warehousing API (Inventory + Topology modules). Owns the "warehouse" database
# (append-only stock ledger) and consumes/publishes integration events via the outbox.
#
# Build context is the REPOSITORY ROOT (the project references SharedKernel, Contracts,
# ServiceDefaults and its modules across directories, plus Directory.*.props at the root):
#   docker build -t warehouse-warehousing-api -f src/Services/Warehousing/Warehouse.Warehousing.Api/Dockerfile .

# --- build stage ----------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
# Version stamped into the assembly (surfaces at GET /version). CI overrides these from
# the release-please tag; the defaults keep a plain `docker build` working locally.
ARG VERSION=0.1.0
ARG GIT_SHA=""
COPY . .
RUN dotnet restore "src/Services/Warehousing/Warehouse.Warehousing.Api/Warehouse.Warehousing.Api.csproj"
RUN dotnet publish "src/Services/Warehousing/Warehouse.Warehousing.Api/Warehouse.Warehousing.Api.csproj" \
    -c Release -o /app/publish --no-restore /p:UseAppHost=false \
    /p:Version=${VERSION} /p:SourceRevisionId=${GIT_SHA}

# --- runtime stage --------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Warehouse.Warehousing.Api.dll"]
