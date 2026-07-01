using Microsoft.EntityFrameworkCore;
using Warehouse.Warehousing.Inventory.Infrastructure;
using Warehouse.Warehousing.Inventory.Infrastructure.Persistence;
using Warehouse.Warehousing.Topology.Infrastructure;
using Warehouse.Warehousing.Topology.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Both contexts share the Warehousing database ("warehouse"), each in its own schema.
builder.AddNpgsqlDbContext<TopologyDbContext>("warehouse");
builder.AddNpgsqlDbContext<InventoryDbContext>("warehouse");
builder.Services.AddTopologyRepositories();
builder.Services.AddInventoryRepositories();

var app = builder.Build();

app.MapDefaultEndpoints();

// Dev convenience: apply migrations on startup. Production uses a migration step in the pipeline.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<TopologyDbContext>().Database.MigrateAsync();
    await scope.ServiceProvider.GetRequiredService<InventoryDbContext>().Database.MigrateAsync();
}

app.MapGet("/", () => "Warehouse Warehousing API");

app.Run();
