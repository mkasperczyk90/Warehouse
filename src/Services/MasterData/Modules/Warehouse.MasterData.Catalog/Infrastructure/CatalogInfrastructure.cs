using Microsoft.Extensions.DependencyInjection;
using Warehouse.MasterData.Catalog.Application.Abstractions;
using Warehouse.MasterData.Catalog.Infrastructure.Persistence;
using Warehouse.SharedKernel.Application;

namespace Warehouse.MasterData.Catalog.Infrastructure;

/// <summary>Registers the Catalog module's repositories. The <see cref="CatalogDbContext"/>
/// itself is registered by the host via Aspire's <c>AddNpgsqlDbContext</c> (which wires the
/// connection string, retries, health checks and telemetry).</summary>
public static class CatalogInfrastructure
{
    public static IServiceCollection AddCatalogRepositories(this IServiceCollection services)
    {
        services.AddScoped<IProductTypeRepository, ProductTypeRepository>();

        // The DbContext is the unit of work; the rename/change-storage handlers commit through this
        // port (one transaction). The define slice commits through the outbox instead.
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<CatalogDbContext>());
        return services;
    }
}
