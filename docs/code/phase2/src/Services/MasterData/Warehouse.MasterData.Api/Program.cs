using JasperFx.Resources;
using Microsoft.EntityFrameworkCore;
using Warehouse.Contracts.Catalog;
using Warehouse.MasterData.Catalog.Application.Abstractions;
using Warehouse.MasterData.Catalog.Domain;
using Warehouse.MasterData.Catalog.Infrastructure;
using Warehouse.MasterData.Catalog.Infrastructure.Persistence;
using Warehouse.MasterData.Partners.Infrastructure;
using Warehouse.MasterData.Partners.Infrastructure.Persistence;
using Warehouse.SharedKernel.ValueObjects;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.Postgresql;
using Wolverine.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Both contexts share the MasterData database ("masterdata"), each in its own schema.
builder.AddNpgsqlDbContext<CatalogDbContext>("masterdata");
builder.AddNpgsqlDbContext<PartnersDbContext>("masterdata");
builder.Services.AddCatalogRepositories();
builder.Services.AddPartnersRepositories();

// Wolverine owns the transactional outbox. Integration events are written to the masterdata
// database in the SAME transaction as the aggregate that produced them, then relayed to RabbitMQ
// after the commit — so "publish after save" can never lose a message, nor send one for a
// transaction that rolled back.
builder.UseWolverine(opts =>
{
    opts.PersistMessagesWithPostgresql(
        builder.Configuration.GetConnectionString("masterdata")!, schemaName: "wolverine");
    opts.UseEntityFrameworkCoreTransactions();

    opts.UseRabbitMq(new Uri(builder.Configuration.GetConnectionString("rabbitmq")!)).AutoProvision();
    opts.PublishMessage<ProductDefinedV1>().ToRabbitExchange("catalog");

    // Durable: the message lands in the outbox table first, so the relay survives a crash.
    opts.Policies.UseDurableOutboxOnAllSendingEndpoints();
});

// Dev: let Wolverine create its message-store tables and provision the RabbitMQ exchange on startup.
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddResourceSetupOnStartup();
}

var app = builder.Build();

app.MapDefaultEndpoints();

// Dev convenience: apply migrations on startup. Production uses a migration step in the pipeline.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<CatalogDbContext>().Database.MigrateAsync();
    await scope.ServiceProvider.GetRequiredService<PartnersDbContext>().Database.MigrateAsync();
}

// Define a product and announce it to the rest of the system through the transactional outbox.
app.MapPost("/catalog/products", async (
    DefineProductRequest request,
    IDbContextOutbox<CatalogDbContext> outbox,
    IProductTypeRepository products,
    CancellationToken cancellationToken) =>
{
    var sku = Sku.Of(request.Sku);
    if (await products.ExistsAsync(sku, cancellationToken))
    {
        return Results.Conflict($"Product {sku} already exists.");
    }

    var product = ProductType.Define(
        sku,
        request.Name,
        ean: null,
        ProductCategory.DryGoods,
        Dimensions.Of(10, 10, 10),
        Weight.FromKilograms(1),
        UnitOfMeasure.FromCode(request.BaseUnit),
        StorageRequirement.Ambient,
        request.IsBatchTracked,
        hasExpiryDate: false);

    products.Add(product);

    // Enqueue the integration event onto this DbContext's outbox...
    await outbox.PublishAsync(new ProductDefinedV1(
        product.Sku.Value,
        product.Name,
        product.BaseUnit.Code,
        product.Storage.RequiresColdChain,
        product.Storage.IsHazardous,
        product.IsBatchTracked,
        DateTimeOffset.UtcNow));

    // ...then commit the product row and the outbox row as ONE transaction; Wolverine relays after.
    await outbox.SaveChangesAndFlushMessagesAsync(cancellationToken);

    return Results.Created($"/catalog/products/{product.Sku.Value}", product.Sku.Value);
});

app.MapGet("/", () => "Warehouse MasterData API");

app.Run();

/// <summary>Request body for defining a product (kept minimal — the focus is the outbox).</summary>
internal sealed record DefineProductRequest(string Sku, string Name, string BaseUnit, bool IsBatchTracked);
