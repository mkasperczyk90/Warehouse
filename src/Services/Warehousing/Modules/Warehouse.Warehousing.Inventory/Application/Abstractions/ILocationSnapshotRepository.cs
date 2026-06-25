using Warehouse.Warehousing.Inventory.Domain;
using Warehouse.Warehousing.Inventory.Domain.Replicas;

namespace Warehouse.Warehousing.Inventory.Application.Abstractions;

/// <summary>
/// Read/write port for the local location replica (fed by Topology's <c>LocationDefined</c> /
/// <c>RoomEnvironmentChanged</c> events). Inventory reads it to enforce put-away invariants without a
/// cross-service query (ADR-0003).
/// </summary>
public interface ILocationSnapshotRepository
{
    Task<LocationSnapshot?> FindAsync(LocationCode code, CancellationToken cancellationToken = default);

    /// <summary>Every location replica in a given room (room codes repeat across warehouses, so it is the pair).</summary>
    Task<IReadOnlyCollection<LocationSnapshot>> ListByRoomAsync(
        WarehouseCode warehouse, string room, CancellationToken cancellationToken = default);

    void Add(LocationSnapshot snapshot);

    void Update(LocationSnapshot snapshot);
}
