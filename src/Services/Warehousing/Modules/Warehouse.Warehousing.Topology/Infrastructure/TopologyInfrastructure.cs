using Microsoft.Extensions.DependencyInjection;
using Warehouse.SharedKernel.Application;
using Warehouse.Warehousing.Topology.Application.Abstractions;
using Warehouse.Warehousing.Topology.Infrastructure.Persistence;

namespace Warehouse.Warehousing.Topology.Infrastructure;

/// <summary>Registers the Topology module's repositories. The <see cref="TopologyDbContext"/> is
/// registered by the host via Aspire's <c>AddNpgsqlDbContext</c>.</summary>
public static class TopologyInfrastructure
{
    public static IServiceCollection AddTopologyRepositories(this IServiceCollection services)
    {
        services.AddScoped<IWarehouseRepository, WarehouseRepository>();

        // The DbContext is the unit of work; the command handlers commit the warehouse aggregate
        // through this port (one transaction per use case).
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<TopologyDbContext>());
        return services;
    }
}
