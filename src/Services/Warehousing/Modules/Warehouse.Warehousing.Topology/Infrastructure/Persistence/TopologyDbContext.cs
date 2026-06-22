using Microsoft.EntityFrameworkCore;
using Warehouse.SharedKernel.Application;
using Warehouse.Warehousing.Topology.Domain;

namespace Warehouse.Warehousing.Topology.Infrastructure.Persistence;

/// <summary>
/// EF Core unit of work for the Topology context. Owns the <c>topology</c> schema inside the
/// Warehousing service's PostgreSQL database (alongside the Inventory context's <c>inventory</c> schema).
/// </summary>
public sealed class TopologyDbContext(DbContextOptions<TopologyDbContext> options)
    : DbContext(options), IUnitOfWork
{
    /// <summary>The schema this context owns inside the shared Warehousing database.</summary>
    public const string Schema = "topology";

    public DbSet<WarehouseSite> Warehouses => Set<WarehouseSite>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TopologyDbContext).Assembly);
    }
}
