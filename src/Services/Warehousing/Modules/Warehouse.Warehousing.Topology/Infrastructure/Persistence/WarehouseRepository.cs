using Microsoft.EntityFrameworkCore;
using Warehouse.Warehousing.Topology.Application.Abstractions;
using Warehouse.Warehousing.Topology.Domain;

namespace Warehouse.Warehousing.Topology.Infrastructure.Persistence;

/// <summary>EF Core implementation of the warehouse persistence port. Rooms, locations and docks
/// are owned, so they load with the aggregate.</summary>
internal sealed class WarehouseRepository(TopologyDbContext context) : IWarehouseRepository
{
    public Task<WarehouseSite?> GetByIdAsync(WarehouseCode id, CancellationToken cancellationToken = default) =>
        context.Warehouses.FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

    public void Add(WarehouseSite aggregate) => context.Warehouses.Add(aggregate);

    public void Update(WarehouseSite aggregate) => context.Warehouses.Update(aggregate);

    public Task<bool> ExistsAsync(WarehouseCode code, CancellationToken cancellationToken = default) =>
        context.Warehouses.AnyAsync(w => w.Id == code, cancellationToken);
}
