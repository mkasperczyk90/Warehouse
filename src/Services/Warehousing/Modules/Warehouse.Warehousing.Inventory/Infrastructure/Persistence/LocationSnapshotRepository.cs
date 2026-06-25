using Microsoft.EntityFrameworkCore;
using Warehouse.Warehousing.Inventory.Application.Abstractions;
using Warehouse.Warehousing.Inventory.Domain;
using Warehouse.Warehousing.Inventory.Domain.Replicas;

namespace Warehouse.Warehousing.Inventory.Infrastructure.Persistence;

/// <summary>EF Core implementation of the location replica port.</summary>
internal sealed class LocationSnapshotRepository(InventoryDbContext context) : ILocationSnapshotRepository
{
    public Task<LocationSnapshot?> FindAsync(LocationCode code, CancellationToken cancellationToken = default) =>
        context.LocationSnapshots.FirstOrDefaultAsync(l => l.Code == code, cancellationToken);

    public async Task<IReadOnlyCollection<LocationSnapshot>> ListByRoomAsync(
        WarehouseCode warehouse, string room, CancellationToken cancellationToken = default) =>
        await context.LocationSnapshots
            .Where(l => l.Warehouse == warehouse && l.Room == room)
            .ToListAsync(cancellationToken);

    public void Add(LocationSnapshot snapshot) => context.LocationSnapshots.Add(snapshot);

    public void Update(LocationSnapshot snapshot) => context.LocationSnapshots.Update(snapshot);
}
