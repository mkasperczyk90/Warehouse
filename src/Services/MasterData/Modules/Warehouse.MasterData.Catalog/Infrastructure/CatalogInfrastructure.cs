using Microsoft.Extensions.DependencyInjection;
using Warehouse.MasterData.Catalog.Application.Abstractions;
using Warehouse.MasterData.Catalog.Infrastructure.Persistence;

namespace Warehouse.MasterData.Catalog.Infrastructure;

/// <summary>Registers the Catalog module's repositories. The <see cref="CatalogDbContext"/>
/// itself is registered by the host via Aspire's <c>AddNpgsqlDbContext</c> (which wires the
/// connection string, retries, health checks and telemetry).</summary>
public static class CatalogInfrastructure
{
    public static IServiceCollection AddCatalogRepositories(this IServiceCollection services)
    {
        services.AddScoped<IProductTypeRepository, ProductTypeRepository>();
        return services;
    }
}
