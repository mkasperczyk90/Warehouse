using Microsoft.EntityFrameworkCore;
using Warehouse.Logistics.Core.Domain;
using Warehouse.SharedKernel.Application;

namespace Warehouse.Logistics.Core.Infrastructure.Persistence;

/// <summary>
/// EF Core unit of work for the Logistics context — the inbound/outbound process sagas. Owns the
/// <c>logistics</c> schema inside the Logistics service's PostgreSQL database. Holds the delivery,
/// order, pick-list and shipment aggregates that track process state; stock itself lives in the
/// Inventory context and is reconciled through integration events.
/// </summary>
public sealed class LogisticsDbContext(DbContextOptions<LogisticsDbContext> options)
    : DbContext(options), IUnitOfWork
{
    /// <summary>The schema this context owns inside the Logistics database.</summary>
    public const string Schema = "logistics";

    public DbSet<InboundDelivery> Deliveries => Set<InboundDelivery>();

    public DbSet<OutboundOrder> Orders => Set<OutboundOrder>();

    public DbSet<PickList> PickLists => Set<PickList>();

    public DbSet<Shipment> Shipments => Set<Shipment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LogisticsDbContext).Assembly);
    }
}
