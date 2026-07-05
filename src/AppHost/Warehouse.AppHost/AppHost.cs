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

// Message broker for integration events (transactional outbox publishes here). Pinned to guest/guest
// (rather than Aspire's default auto-generated password) for two reasons: it matches the ECS
// deployment's own RabbitMQ container (infra/terraform/environment/locals.tf — same default, never
// overridden there either), and Warehouse.CdcBridge below reads RabbitMQ host/user/password as plain
// CdcBridgeOptions config rather than an Aspire-injected connection string, so it needs a credential
// it can default to without extra plumbing.
var rabbitmqUser = builder.AddParameter("rabbitmq-username", "guest");
var rabbitmqPassword = builder.AddParameter("rabbitmq-password", "guest", secret: true);
var rabbitmq = builder.AddRabbitMQ("rabbitmq", rabbitmqUser, rabbitmqPassword);

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

// Each service validates the Keycloak JWT itself (zero-trust), so it needs the same resolved realm URL
// (Keycloak__Authority) + audience (Keycloak__ClientId) the gateway uses, and must wait for Keycloak so
// its OIDC metadata/JWKS is reachable.
var keycloakAuthority = ReferenceExpression.Create($"{keycloak.GetEndpoint("http")}/realms/warehouse");

var masterData = builder.AddProject<Projects.Warehouse_MasterData_Api>("masterdata-api")
    .WithReference(masterDataDb).WaitFor(masterDataDb)
    .WithReference(rabbitmq).WaitFor(rabbitmq)
    .WithEnvironment("Keycloak__Authority", keycloakAuthority)
    .WithEnvironment("Keycloak__ClientId", "warehouse-admin")
    .WaitFor(keycloak);

var warehousing = builder.AddProject<Projects.Warehouse_Warehousing_Api>("warehousing-api")
    .WithReference(warehouseDb).WaitFor(warehouseDb)
    .WithReference(rabbitmq).WaitFor(rabbitmq)
    .WithEnvironment("Keycloak__Authority", keycloakAuthority)
    .WithEnvironment("Keycloak__ClientId", "warehouse-admin")
    .WaitFor(keycloak);

var logistics = builder.AddProject<Projects.Warehouse_Logistics_Api>("logistics-api")
    .WithReference(logisticsDb).WaitFor(logisticsDb)
    .WithReference(rabbitmq).WaitFor(rabbitmq)
    .WithEnvironment("Keycloak__Authority", keycloakAuthority)
    .WithEnvironment("Keycloak__ClientId", "warehouse-admin")
    .WaitFor(keycloak);

// API gateway (YARP) fronts the three services and validates the Keycloak JWTs.
var gateway = builder.AddProject<Projects.Warehouse_Gateway>("gateway")
    .WithReference(masterData)
    .WithReference(warehousing)
    .WithReference(logistics)
    .WithReference(keycloak.GetEndpoint("http")).WaitFor(keycloak)
    // The *resolved* realm URL (host-reachable), not the logical 'http://keycloak' name: the gateway's
    // JwtBearer metadata backchannel doesn't run through service discovery, and the broker mints tokens
    // from this same URL so issuer + signature validation line up.
    .WithEnvironment("Keycloak__Authority",
        ReferenceExpression.Create($"{keycloak.GetEndpoint("http")}/realms/warehouse"))
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

// Bronze/BI workers (docs/BI_Plan.md §11 + CDC section) — the ECS deployment's other consumers of
// the shared task role's S3 write access. Neither writes anything locally (BiExport/CdcBridge's S3
// writer both no-op-by-omission without a configured bucket, the same "off by default" behaviour the
// 3 APIs' own AddBiExport() call already has), but both are wired in so `dotnet run` on this AppHost
// actually launches every service that ships, not just the ones with an HTTP endpoint to click on.
//
// identity-bridge only needs Keycloak, which is already here — its Admin API poll works for real
// locally (AdminUsername/AdminPassword default to "admin"/"admin", matching this AppHost's own
// KC_BOOTSTRAP_ADMIN_USERNAME/PASSWORD above).
var identityBridge = builder.AddProject<Projects.Warehouse_IdentityBridge>("identity-bridge")
    .WithEnvironment("IdentityBridge__KeycloakBaseUrl", keycloak.GetEndpoint("http"))
    .WaitFor(keycloak);

// cdc-bridge's actual data source (Debezium Server reading Postgres logical replication) is an ECS/
// Terraform-only piece (infra/terraform/environment/locals.tf) — porting 3 Debezium JVM instances
// plus wal_level=logical into local Aspire orchestration is a separate, bigger undertaking than
// "launch every project", so it isn't done here. Still wired in (rather than left out entirely) so
// the process itself starts and is exercised locally: it connects to RabbitMQ and declares/binds its
// 3 CDC exchanges/queues itself now (CdcListenerService no longer assumes rabbitmq-init ran first),
// then idles — harmlessly, since nothing publishes to those exchanges without Debezium.
// CdcBridgeOptions binds RabbitMQ host/port/user/password as plain config, not an Aspire connection
// string (WithReference alone would only inject ConnectionStrings__rabbitmq, which CdcBridgeOptions
// doesn't read) — so host/port come from the resolved "tcp" endpoint explicitly, and user/password
// reuse the same parameters the rabbitmq resource itself was pinned to above.
var rabbitmqEndpoint = rabbitmq.GetEndpoint("tcp");
var cdcBridge = builder.AddProject<Projects.Warehouse_CdcBridge>("cdc-bridge")
    .WithReference(rabbitmq).WaitFor(rabbitmq)
    .WithEnvironment("CdcBridge__RabbitMqHost", rabbitmqEndpoint.Property(EndpointProperty.Host))
    .WithEnvironment("CdcBridge__RabbitMqPort", rabbitmqEndpoint.Property(EndpointProperty.Port))
    .WithEnvironment("CdcBridge__RabbitMqUsername", rabbitmqUser)
    .WithEnvironment("CdcBridge__RabbitMqPassword", rabbitmqPassword)
    .WithEnvironment("CdcBridge__Exchanges__0", "cdc.warehouse")
    .WithEnvironment("CdcBridge__Exchanges__1", "cdc.logistics")
    .WithEnvironment("CdcBridge__Exchanges__2", "cdc.masterdata");

builder.Build().Run();
