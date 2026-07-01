using Microsoft.Extensions.DependencyInjection;
using Warehouse.Logistics.Core.Application.Abstractions;
using Warehouse.Logistics.Core.Infrastructure.Persistence;

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
        return services;
    }
}
