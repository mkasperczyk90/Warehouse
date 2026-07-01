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
builder.AddProject<Projects.Warehouse_Gateway>("gateway")
    .WithReference(masterData)
    .WithReference(warehousing)
    .WithReference(logistics);

builder.Build().Run();
