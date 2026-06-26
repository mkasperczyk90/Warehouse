var builder = DistributedApplication.CreateBuilder(args);

// One PostgreSQL server, one database per service (ADR: database-per-service).
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();

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
    .WithArgs("start-dev", "--import-realm");

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

// Front-ends — MSW-mocked SPAs served by nginx, built from their own Dockerfiles (ADR-0004,
// ADR-0006). They run standalone today (the mock worker answers fetch); the gateway reference is
// the seam where a real API base URL attaches when MSW is switched off.
builder.AddDockerfile("admin", "../../web/admin")
    .WithReference(gateway)
    .WithHttpEndpoint(targetPort: 80)
    .WithExternalHttpEndpoints();

builder.AddDockerfile("terminal", "../../web/terminal")
    .WithReference(gateway)
    .WithHttpEndpoint(targetPort: 80)
    .WithExternalHttpEndpoints();

builder.Build().Run();
