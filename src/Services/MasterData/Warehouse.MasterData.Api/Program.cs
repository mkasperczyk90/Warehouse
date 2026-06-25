using Microsoft.EntityFrameworkCore;
using Warehouse.Contracts.Catalog;
using Warehouse.MasterData.Api;
using Warehouse.MasterData.Catalog.Application;
using Warehouse.MasterData.Catalog.Application.Products.ImportProducts;
using Warehouse.MasterData.Catalog.Infrastructure;
using Warehouse.MasterData.Catalog.Infrastructure.Persistence;
using Warehouse.MasterData.Partners.Infrastructure;
using Warehouse.MasterData.Partners.Infrastructure.Persistence;
using Wolverine.RabbitMQ;
using ExchangeType = Wolverine.RabbitMQ.ExchangeType;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Both contexts share the MasterData database ("masterdata"), each in its own schema.
builder.AddNpgsqlDbContext<CatalogDbContext>("masterdata");
builder.AddNpgsqlDbContext<PartnersDbContext>("masterdata");
builder.Services.AddCatalogRepositories();
builder.Services.AddCatalogApplication();
builder.Services.AddPartnersRepositories();

builder.Services.AddExceptionHandler<DomainExceptionHandler>();
builder.Services.AddProblemDetails();

// Transactional outbox over the masterdata database (shared wiring in ServiceDefaults).
// MasterData only publishes — it announces catalog changes to the rest of the system.
builder.AddWarehouseMessaging("masterdata", (opts, rabbit) =>
{
    // Broadcast catalog changes on a fanout exchange; each interested service binds its own queue.
    opts.PublishMessage<ProductDefinedV2>()
        .ToRabbitExchange("catalog", e => e.ExchangeType = ExchangeType.Fanout);
});

var app = builder.Build();

app.UseExceptionHandler();

app.MapDefaultEndpoints();

// Dev convenience: apply migrations on startup. Production uses a migration step in the pipeline.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var catalogDb = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    await catalogDb.Database.MigrateAsync();
    await scope.ServiceProvider.GetRequiredService<PartnersDbContext>().Database.MigrateAsync();

    // Seed the catalog (idempotent) so the admin panel has real product cards to browse, and so the
    // resulting ProductDefinedV2 events build the Inventory/Logistics replicas.
    await CatalogSeeder.SeedAsync(
        scope.ServiceProvider.GetRequiredService<ImportProductsHandler>(), catalogDb);
}

app.MapCatalogEndpoints();

app.MapGet("/", () => "Warehouse MasterData API");

app.Run();
