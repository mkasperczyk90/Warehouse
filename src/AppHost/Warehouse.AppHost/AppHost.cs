var builder = DistributedApplication.CreateBuilder(args);

// One PostgreSQL server, one database per service (ADR: database-per-service).
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();

var masterDataDb = postgres.AddDatabase("masterdata");
var warehouseDb = postgres.AddDatabase("warehouse");
var logisticsDb = postgres.AddDatabase("logistics");

// Message broker for integration events (transactional outbox publishes here).
var rabbitmq = builder.AddRabbitMQ("rabbitmq");

var masterData = builder.AddProject<Projects.Warehouse_MasterData_Api>("masterdata-api")
    .WithReference(masterDataDb).WaitFor(masterDataDb)
    .WithReference(rabbitmq).WaitFor(rabbitmq);

var warehousing = builder.AddProject<Projects.Warehouse_Warehousing_Api>("warehousing-api")
    .WithReference(warehouseDb).WaitFor(warehouseDb)
    .WithReference(rabbitmq).WaitFor(rabbitmq);

var logistics = builder.AddProject<Projects.Warehouse_Logistics_Api>("logistics-api")
    .WithReference(logisticsDb).WaitFor(logisticsDb)
    .WithReference(rabbitmq).WaitFor(rabbitmq);

// API gateway (YARP) fronts the three services.
var gateway = builder.AddProject<Projects.Warehouse_Gateway>("gateway")
    .WithReference(masterData)
    .WithReference(warehousing)
    .WithReference(logistics);

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
