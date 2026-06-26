using Microsoft.EntityFrameworkCore;
using Warehouse.Contracts.Logistics;
using Warehouse.Contracts.Topology;
using Warehouse.Warehousing.Api;
using Warehouse.Warehousing.Inventory.Application;
using Warehouse.Warehousing.Inventory.Infrastructure;
using Warehouse.Warehousing.Inventory.Infrastructure.Persistence;
using Warehouse.Warehousing.Topology.Application;
using Warehouse.Warehousing.Topology.Infrastructure;
using Warehouse.Warehousing.Topology.Infrastructure.Persistence;
using Wolverine;
using Wolverine.RabbitMQ;
using ExchangeType = Wolverine.RabbitMQ.ExchangeType;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Both contexts share the Warehousing database ("warehouse"), each in its own schema. Retry is disabled:
// command handlers open their own transaction via the Wolverine outbox (SaveChangesAndFlushMessagesAsync),
// which an Npgsql retrying execution strategy refuses ("does not support user-initiated transactions").
// Durability is covered by the durable outbox + Wolverine's message-level retries instead.
builder.AddNpgsqlDbContext<TopologyDbContext>("warehouse", settings => settings.DisableRetry = true);
builder.AddNpgsqlDbContext<InventoryDbContext>("warehouse", settings => settings.DisableRetry = true);
builder.Services.AddTopologyRepositories();
builder.Services.AddTopologyApplication();
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

    // Topology announces locations/room-environment changes; Inventory keeps a LocationSnapshot replica.
    // Both contexts live in this service, so the stream loops back through its own queue (forward-compatible
    // with splitting Topology into its own service later).
    rabbit.BindExchange("topology", ExchangeType.Fanout).ToQueue("warehousing.topology");
    opts.ListenToRabbitQueue("warehousing.topology");

    // Publish the Topology stream on its own fanout exchange.
    opts.PublishMessage<LocationDefinedV1>()
        .ToRabbitExchange("topology", e => e.ExchangeType = ExchangeType.Fanout);
    opts.PublishMessage<RoomEnvironmentChangedV1>()
        .ToRabbitExchange("topology", e => e.ExchangeType = ExchangeType.Fanout);

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

// Dev convenience: apply migrations on startup and seed a demo dataset. Production uses a migration
// step in the pipeline and never seeds.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var topology = scope.ServiceProvider.GetRequiredService<TopologyDbContext>();
    var inventory = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
    await topology.Database.MigrateAsync();
    await inventory.Database.MigrateAsync();
    await TopologySeeder.SeedAsync(topology);
    await InventorySeeder.SeedAsync(inventory);
}

app.MapTopologyEndpoints();
app.MapInventoryEndpoints();
app.MapStockEndpoints();
app.MapStocktakeEndpoints();
app.MapQualityEndpoints();

app.MapGet("/", () => "Warehouse Warehousing API");

app.Run();
