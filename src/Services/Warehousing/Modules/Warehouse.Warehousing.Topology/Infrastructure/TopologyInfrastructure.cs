using Microsoft.Extensions.DependencyInjection;
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
        return services;
    }
}
