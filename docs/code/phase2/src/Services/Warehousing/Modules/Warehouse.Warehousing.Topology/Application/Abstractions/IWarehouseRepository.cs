using Warehouse.SharedKernel.Application;
using Warehouse.Warehousing.Topology.Domain;

namespace Warehouse.Warehousing.Topology.Application.Abstractions;

/// <summary>Persistence port for the <see cref="WarehouseSite"/> aggregate (rooms, locations, docks).</summary>
public interface IWarehouseRepository : IRepository<WarehouseSite, WarehouseCode>
{
    /// <summary>True if a warehouse with this code already exists (uniqueness probe).</summary>
    Task<bool> ExistsAsync(WarehouseCode code, CancellationToken cancellationToken = default);
}
