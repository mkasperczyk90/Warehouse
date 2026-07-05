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
builder.AddBiExport(); // BI export to the S3 Bronze landing zone (docs/BI_Plan.md §11) — no-op unless "BiExport:BucketName" is configured.

// Zero-trust: validate the Keycloak JWT here too (not just at the gateway). The fallback policy makes every
// business endpoint require an authenticated warehouse role; the gateway forwards the caller's bearer.
builder.AddWarehouseJwtAuth(requireAuthenticatedByDefault: true);

// Logistics owns its own database ("logistics", ADR: database-per-service). Retry is disabled: command
// handlers open their own transaction via the Wolverine outbox (SaveChangesAndFlushMessagesAsync), which
// an Npgsql retrying execution strategy refuses ("does not support user-initiated transactions").
// Durability is covered by the durable outbox + Wolverine's message-level retries instead.
builder.AddNpgsqlDbContext<LogisticsDbContext>("logistics", settings => settings.DisableRetry = true);
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
    // Delivery timeline facts (announced/dock-slot/arrived) have no other in-service consumer today —
    // they only exist to feed BI export (docs/BI_Plan.md §5.3), same exchange as everything else here.
    opts.PublishMessage<DeliveryAnnouncedV1>()
        .ToRabbitExchange("logistics", e => e.ExchangeType = ExchangeType.Fanout);
    opts.PublishMessage<DockSlotAssignedV1>()
        .ToRabbitExchange("logistics", e => e.ExchangeType = ExchangeType.Fanout);
    opts.PublishMessage<DeliveryArrivedV1>()
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

    // BI export (docs/BI_Plan.md §11): a dedicated queue on the same "logistics" exchange this
    // service already publishes to above — decoupled from the business-critical
    // "logistics.catalog"/"logistics.inventory" queues, so a slow/backed-up export can never touch
    // the domain flow.
    rabbit.BindExchange("logistics", ExchangeType.Fanout).ToQueue("logistics.bi-export");
    opts.ListenToRabbitQueue("logistics.bi-export");

    // Handlers (slice + consumers) live in the Core module assembly.
    opts.Discovery.IncludeAssembly(typeof(LogisticsDbContext).Assembly);
});

var app = builder.Build();

app.UseExceptionHandler();

app.UseAuthentication();
app.UseAuthorization();

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

app.MapGet("/", () => "Warehouse Logistics API").AllowAnonymous();

app.Run();
