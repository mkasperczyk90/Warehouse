var builder = DistributedApplication.CreateBuilder(args);

// One PostgreSQL server, one database per service (ADR: database-per-service).
// No data volume: each `dotnet run` starts on a fresh database so the per-service seeders re-run and
// re-publish their integration events — the event-fed read replicas (catalog/topology snapshots) only
// populate through those events, so a persisted volume from an earlier (broken-messaging) run would
// leave them half-empty. Ephemeral is the right default for the local demo stack.
var postgres = builder.AddPostgres("postgres");

var masterDataDb = postgres.AddDatabase("masterdata");
var warehouseDb = postgres.AddDatabase("warehouse");
var logisticsDb = postgres.AddDatabase("logistics");

// Message broker for integration events (transactional outbox publishes here).
var rabbitmq = builder.AddRabbitMQ("rabbitmq");

// Identity provider (Keycloak): hosts the 'warehouse' realm (roles + desk users) and the custom badge
// Direct-Grant authenticator, so the desk's badge-scan sign-in issues real JWTs. Provisioned as a raw
// container (the Aspire Keycloak integration is preview-only): the realm import folder and the SPI jar
// (built by Maven first — see src/Identity/keycloak-badge-authenticator) are bind-mounted in, and
// `start-dev --import-realm` loads both. The image tag must match the SPI's keycloak.version. The gateway
// validates these tokens and brokers the badge sign-in.
var keycloak = builder.AddContainer("keycloak", "quay.io/keycloak/keycloak", "26.0.7")
    .WithHttpEndpoint(targetPort: 8080, name: "http")
    .WithEnvironment("KC_BOOTSTRAP_ADMIN_USERNAME", "admin")
    .WithEnvironment("KC_BOOTSTRAP_ADMIN_PASSWORD", "admin")
    .WithBindMount("../../Identity/realms", "/opt/keycloak/data/import")
    .WithBindMount(
        "../../Identity/keycloak-badge-authenticator/target/badge-authenticator.jar",
        "/opt/keycloak/providers/badge-authenticator.jar")
    .WithArgs("start-dev", "--import-realm")
    // Readiness: the realm's OIDC discovery doc only returns 200 once 'warehouse' has finished importing,
    // so dependents that WaitFor(keycloak) (the gateway) hold until tokens can actually be issued/validated,
    // not merely until the container is running.
    .WithHttpHealthCheck("/realms/warehouse/.well-known/openid-configuration", endpointName: "http");

var masterData = builder.AddProject<Projects.Warehouse_MasterData_Api>("masterdata-api")
    .WithReference(masterDataDb).WaitFor(masterDataDb)
    .WithReference(rabbitmq).WaitFor(rabbitmq);

var warehousing = builder.AddProject<Projects.Warehouse_Warehousing_Api>("warehousing-api")
    .WithReference(warehouseDb).WaitFor(warehouseDb)
    .WithReference(rabbitmq).WaitFor(rabbitmq);

var logistics = builder.AddProject<Projects.Warehouse_Logistics_Api>("logistics-api")
    .WithReference(logisticsDb).WaitFor(logisticsDb)
    .WithReference(rabbitmq).WaitFor(rabbitmq);

// API gateway (YARP) fronts the three services and validates the Keycloak JWTs.
var gateway = builder.AddProject<Projects.Warehouse_Gateway>("gateway")
    .WithReference(masterData)
    .WithReference(warehousing)
    .WithReference(logistics)
    .WithReference(keycloak.GetEndpoint("http")).WaitFor(keycloak)
    .WithEnvironment("Keycloak__Realm", "warehouse")
    .WithEnvironment("Keycloak__ClientId", "warehouse-admin")
    .WithEnvironment("Keycloak__ClientSecret", "warehouse-admin-secret-dev");

// Front-ends — SPAs served by nginx, built from their own Dockerfiles (ADR-0004, ADR-0006). Both proxy
// `/api` to the gateway (the `GATEWAY_UPSTREAM` the Dockerfile's nginx template expands). The admin still
// ships MSW and is built with it switched OFF here (build arg); the terminal no longer uses MSW and always
// calls the real gateway. Build-arg changes trigger an image rebuild on `dotnet run`.
builder.AddDockerfile("admin", "../../web/admin")
    .WithBuildArg("VITE_USE_MOCKS", "false")
    .WithReference(gateway)
    .WithEnvironment("GATEWAY_UPSTREAM", gateway.GetEndpoint("http"))
    .WithHttpEndpoint(targetPort: 80)
    .WithExternalHttpEndpoints();

builder.AddDockerfile("terminal", "../../web/terminal")
    .WithReference(gateway)
    .WithEnvironment("GATEWAY_UPSTREAM", gateway.GetEndpoint("http"))
    .WithHttpEndpoint(targetPort: 80)
    .WithExternalHttpEndpoints();

builder.Build().Run();
