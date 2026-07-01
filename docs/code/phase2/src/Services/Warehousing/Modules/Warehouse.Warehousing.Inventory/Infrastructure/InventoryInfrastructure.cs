using Microsoft.Extensions.DependencyInjection;
using Warehouse.Warehousing.Inventory.Application.Abstractions;
using Warehouse.Warehousing.Inventory.Infrastructure.Persistence;

namespace Warehouse.Warehousing.Inventory.Infrastructure;

/// <summary>Registers the Inventory module's repositories. The <see cref="InventoryDbContext"/> is
/// registered by the host via Aspire's <c>AddNpgsqlDbContext</c> (which wires the connection
/// string, retries, health checks and telemetry).</summary>
public static class InventoryInfrastructure
{
    public static IServiceCollection AddInventoryRepositories(this IServiceCollection services)
    {
        services.AddScoped<IStockItemRepository, StockItemRepository>();
        services.AddScoped<IBatchRepository, BatchRepository>();
        services.AddScoped<IStockReservationRepository, StockReservationRepository>();
        return services;
    }
}
