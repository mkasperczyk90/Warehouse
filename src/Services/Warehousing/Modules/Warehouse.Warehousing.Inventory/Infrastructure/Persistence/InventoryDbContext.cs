using Microsoft.EntityFrameworkCore;
using Warehouse.SharedKernel.Application;
using Warehouse.Warehousing.Inventory.Domain;
using Warehouse.Warehousing.Inventory.Domain.Replicas;

namespace Warehouse.Warehousing.Inventory.Infrastructure.Persistence;

/// <summary>
/// EF Core unit of work for the Inventory context — the core domain. Owns the <c>inventory</c>
/// schema inside the Warehousing service's PostgreSQL database (alongside Topology's
/// <c>topology</c> schema). Holds the stock aggregates, the append-only movement ledger, and the
/// local replicas of product/location data kept fresh by integration events.
/// </summary>
public sealed class InventoryDbContext(DbContextOptions<InventoryDbContext> options)
    : DbContext(options), IUnitOfWork
{
    /// <summary>The schema this context owns inside the shared Warehousing database.</summary>
    public const string Schema = "inventory";

    public DbSet<StockItem> StockItems => Set<StockItem>();

    public DbSet<Batch> Batches => Set<Batch>();

    public DbSet<StockReservation> StockReservations => Set<StockReservation>();

    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

    public DbSet<Stocktake> Stocktakes => Set<Stocktake>();

    public DbSet<HandlingUnit> HandlingUnits => Set<HandlingUnit>();

    public DbSet<ProductSnapshot> ProductSnapshots => Set<ProductSnapshot>();

    public DbSet<LocationSnapshot> LocationSnapshots => Set<LocationSnapshot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InventoryDbContext).Assembly);
    }
}
