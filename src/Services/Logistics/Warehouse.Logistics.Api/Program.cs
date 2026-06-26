using Microsoft.EntityFrameworkCore;
using Warehouse.Contracts.Logistics;
using Warehouse.Logistics.Api;
using Warehouse.Logistics.Core.Application;
using Warehouse.Logistics.Core.Infrastructure;
using Warehouse.Logistics.Core.Infrastructure.Persistence;
using Wolverine;
using Wolverine.RabbitMQ;
using ExchangeType = Wolverine.RabbitMQ.ExchangeType;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Logistics owns its own database ("logistics", ADR: database-per-service).
builder.AddNpgsqlDbContext<LogisticsDbContext>("logistics");
builder.Services.AddLogisticsRepositories();
builder.Services.AddLogisticsApplication();

builder.Services.AddExceptionHandler<DomainExceptionHandler>();
builder.Services.AddProblemDetails();

// Transactional outbox over the logistics database (shared wiring in ServiceDefaults).
builder.AddWarehouseMessaging("logistics", (opts, rabbit) =>
{
    // Consume the Catalog stream (SKU validation replica) and Inventory's inbound replies.
    rabbit.BindExchange("catalog", ExchangeType.Fanout).ToQueue("logistics.catalog");
    rabbit.BindExchange("inventory", ExchangeType.Fanout).ToQueue("logistics.inventory");
    opts.ListenToRabbitQueue("logistics.catalog");
    opts.ListenToRabbitQueue("logistics.inventory");

    // Announce inbound goods receipts and outbound order/dispatch facts to Inventory.
    opts.PublishMessage<GoodsReceiptConfirmedV1>()
        .ToRabbitExchange("logistics", e => e.ExchangeType = ExchangeType.Fanout);
    opts.PublishMessage<OutboundOrderPlacedV1>()
        .ToRabbitExchange("logistics", e => e.ExchangeType = ExchangeType.Fanout);
    opts.PublishMessage<OutboundOrderCancelledV1>()
        .ToRabbitExchange("logistics", e => e.ExchangeType = ExchangeType.Fanout);
    opts.PublishMessage<ShipmentDispatchedV1>()
        .ToRabbitExchange("logistics", e => e.ExchangeType = ExchangeType.Fanout);
    opts.PublishMessage<PickingReleasedV1>()
        .ToRabbitExchange("logistics", e => e.ExchangeType = ExchangeType.Fanout);
    opts.PublishMessage<PickConfirmedV1>()
        .ToRabbitExchange("logistics", e => e.ExchangeType = ExchangeType.Fanout);

    // Handlers (slice + consumers) live in the Core module assembly.
    opts.Discovery.IncludeAssembly(typeof(LogisticsDbContext).Assembly);
});

var app = builder.Build();

app.UseExceptionHandler();

app.MapDefaultEndpoints();

// Dev convenience: apply migrations on startup. Production uses a migration step in the pipeline.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var logistics = scope.ServiceProvider.GetRequiredService<LogisticsDbContext>();
    await logistics.Database.MigrateAsync();
    // Seed announced deliveries + placed orders (idempotent) so Inbound/Outbound show real data.
    await LogisticsSeeder.SeedAsync(logistics);
}

app.MapInboundEndpoints();
app.MapOutboundEndpoints();
app.MapDispatchEndpoints();

app.MapGet("/", () => "Warehouse Logistics API");

app.Run();
