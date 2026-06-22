using Microsoft.Extensions.DependencyInjection;
using Warehouse.Logistics.Core.Application.Abstractions;
using Warehouse.Logistics.Core.Infrastructure.Persistence;
using Warehouse.SharedKernel.Application;

namespace Warehouse.Logistics.Core.Infrastructure;

/// <summary>Registers the Logistics module's repositories. The <see cref="LogisticsDbContext"/> is
/// registered by the host via Aspire's <c>AddNpgsqlDbContext</c> (which wires the connection
/// string, retries, health checks and telemetry).</summary>
public static class LogisticsInfrastructure
{
    public static IServiceCollection AddLogisticsRepositories(this IServiceCollection services)
    {
        services.AddScoped<IInboundDeliveryRepository, InboundDeliveryRepository>();
        services.AddScoped<IOutboundOrderRepository, OutboundOrderRepository>();
        services.AddScoped<IPickListRepository, PickListRepository>();
        services.AddScoped<IShipmentRepository, ShipmentRepository>();
        services.AddScoped<ICatalogProductReplica, CatalogProductReplicaRepository>();

        // The DbContext is the unit of work; handlers commit through this port (one transaction).
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<LogisticsDbContext>());
        return services;
    }
}
