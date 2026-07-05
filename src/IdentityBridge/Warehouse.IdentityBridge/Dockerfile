# Warehouse Identity Bridge — polls Keycloak's Admin REST API for the user/role list on a fixed
# interval and lands a BronzeUser snapshot per user (docs/BI_Plan.md §5.4). No HTTP surface: a
# worker, not an API. Keycloak has no persistent database in this stack (dev-mode), so this is the
# only path Identity data has into Bronze — see the CDC section in docs/BI_Plan.md for why.
#
# Build context is the REPOSITORY ROOT (references ServiceDefaults + Directory.*.props at the root):
#   docker build -t warehouse-identity-bridge -f src/IdentityBridge/Warehouse.IdentityBridge/Dockerfile .

# --- build stage ----------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
ARG VERSION=0.1.0
ARG GIT_SHA=""
COPY . .
RUN dotnet restore "src/IdentityBridge/Warehouse.IdentityBridge/Warehouse.IdentityBridge.csproj"
RUN dotnet publish "src/IdentityBridge/Warehouse.IdentityBridge/Warehouse.IdentityBridge.csproj" \
    -c Release -o /app/publish --no-restore /p:UseAppHost=false \
    /p:Version=${VERSION} /p:SourceRevisionId=${GIT_SHA}

# --- runtime stage --------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Warehouse.IdentityBridge.dll"]
