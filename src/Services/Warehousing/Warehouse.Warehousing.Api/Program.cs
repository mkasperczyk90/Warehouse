using Microsoft.EntityFrameworkCore;
using Warehouse.Contracts.Logistics;
using Warehouse.Warehousing.Api;
using Warehouse.Warehousing.Inventory.Application;
using Warehouse.Warehousing.Inventory.Infrastructure;
using Warehouse.Warehousing.Inventory.Infrastructure.Persistence;
using Warehouse.Warehousing.Topology.Infrastructure;
using Warehouse.Warehousing.Topology.Infrastructure.Persistence;
using Wolverine;
using Wolverine.RabbitMQ;
using ExchangeType = Wolverine.RabbitMQ.ExchangeType;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Both contexts share the Warehousing database ("warehouse"), each in its own schema.
builder.AddNpgsqlDbContext<TopologyDbContext>("warehouse");
builder.AddNpgsqlDbContext<InventoryDbContext>("warehouse");
builder.Services.AddTopologyRepositories();
builder.Services.AddInventoryRepositories();
builder.Services.AddInventoryApplication();

builder.Services.AddExceptionHandler<DomainExceptionHandler>();
builder.Services.AddProblemDetails();

// Transactional outbox over the warehouse database (shared wiring in ServiceDefaults). The outbox
// lives in the inventory schema's database, alongside the stock it reacts to.
builder.AddWarehouseMessaging("warehouse", (opts, rabbit) =>
{
    // Consume the Catalog stream (product replica) and Logistics' goods-receipt stream.
    rabbit.BindExchange("catalog", ExchangeType.Fanout).ToQueue("warehousing.catalog");
    rabbit.BindExchange("logistics", ExchangeType.Fanout).ToQueue("warehousing.logistics");
    opts.ListenToRabbitQueue("warehousing.catalog");
    opts.ListenToRabbitQueue("warehousing.logistics");

    // Reply to Logistics: goods received, put-away complete (inbound), and stock reserved (outbound).
    opts.PublishMessage<GoodsReceivedV1>()
        .ToRabbitExchange("inventory", e => e.ExchangeType = ExchangeType.Fanout);
    opts.PublishMessage<PutAwayCompletedV1>()
        .ToRabbitExchange("inventory", e => e.ExchangeType = ExchangeType.Fanout);
    opts.PublishMessage<StockReservedV1>()
        .ToRabbitExchange("inventory", e => e.ExchangeType = ExchangeType.Fanout);
    opts.PublishMessage<PicksPlannedV1>()
        .ToRabbitExchange("inventory", e => e.ExchangeType = ExchangeType.Fanout);

    // Inventory handlers (consumers + slices) live in the Inventory module assembly.
    opts.Discovery.IncludeAssembly(typeof(InventoryDbContext).Assembly);
});

var app = builder.Build();

app.UseExceptionHandler();

app.MapDefaultEndpoints();

// Dev convenience: apply migrations on startup. Production uses a migration step in the pipeline.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<TopologyDbContext>().Database.MigrateAsync();
    await scope.ServiceProvider.GetRequiredService<InventoryDbContext>().Database.MigrateAsync();
}

app.MapInventoryEndpoints();

app.MapGet("/", () => "Warehouse Warehousing API");

app.Run();
