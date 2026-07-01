using Microsoft.EntityFrameworkCore;
using Warehouse.Logistics.Core.Infrastructure;
using Warehouse.Logistics.Core.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Logistics owns its own database ("logistics", ADR: database-per-service).
builder.AddNpgsqlDbContext<LogisticsDbContext>("logistics");
builder.Services.AddLogisticsRepositories();

var app = builder.Build();

app.MapDefaultEndpoints();

// Dev convenience: apply migrations on startup. Production uses a migration step in the pipeline.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<LogisticsDbContext>().Database.MigrateAsync();
}

app.MapGet("/", () => "Warehouse Logistics API");

app.Run();
