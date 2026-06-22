using Microsoft.EntityFrameworkCore;
using Warehouse.MasterData.Catalog.Domain;
using Warehouse.SharedKernel.Application;

namespace Warehouse.MasterData.Catalog.Infrastructure.Persistence;

/// <summary>
/// EF Core unit of work for the Catalog context. Owns the <c>catalog</c> schema inside the
/// MasterData service's PostgreSQL database; the Partners context lives in its own schema in the
/// same database. Satisfies <see cref="IUnitOfWork"/> through the inherited
/// <see cref="DbContext.SaveChangesAsync(CancellationToken)"/>.
/// </summary>
public sealed class CatalogDbContext(DbContextOptions<CatalogDbContext> options)
    : DbContext(options), IUnitOfWork
{
    /// <summary>The schema this context owns inside the shared MasterData database.</summary>
    public const string Schema = "catalog";

    public DbSet<ProductType> Products => Set<ProductType>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogDbContext).Assembly);
    }
}
