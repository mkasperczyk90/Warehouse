using Warehouse.SharedKernel.Application;
using Warehouse.Warehousing.Inventory.Application.Abstractions;
using Warehouse.Warehousing.Inventory.Domain;
using Warehouse.Warehousing.Inventory.Domain.Replicas;

namespace Warehouse.Warehousing.Tests.TestDoubles;

internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveCount { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveCount++;
        return Task.FromResult(1);
    }
}

/// <summary>In-memory <see cref="IStockItemRepository"/>. Aggregates mutate by reference, so the
/// store just holds the instances and the query methods filter over them.</summary>
internal sealed class FakeStockItemRepository : IStockItemRepository
{
    private readonly List<StockItem> _items = [];

    public void Seed(params StockItem[] items) => _items.AddRange(items);

    public Task<StockItem?> GetByIdAsync(StockItemId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_items.SingleOrDefault(i => i.Id == id));

    public void Add(StockItem aggregate) => _items.Add(aggregate);

    public void Update(StockItem aggregate)
    {
        // Reference semantics: the instance is already in the list.
    }

    public Task<StockItem?> GetAtAsync(Sku sku, BatchNumber? batch, LocationCode location, CancellationToken cancellationToken = default) =>
        Task.FromResult(_items.SingleOrDefault(i => i.Sku == sku && i.Batch == batch && i.Location == location));

    public Task<IReadOnlyCollection<StockItem>> ListBySkuAsync(Sku sku, WarehouseCode warehouse, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<StockItem>>(_items.Where(i => i.Sku == sku).ToList());

    public Task<IReadOnlyCollection<StockItem>> ListAtAsync(LocationCode location, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<StockItem>>(_items.Where(i => i.Location == location).ToList());
}

internal sealed class FakeStockReservationRepository : IStockReservationRepository
{
    private readonly List<StockReservation> _reservations = [];

    public IReadOnlyList<StockReservation> All => _reservations;

    public void Seed(params StockReservation[] reservations) => _reservations.AddRange(reservations);

    public Task<StockReservation?> GetByIdAsync(StockReservationId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_reservations.SingleOrDefault(r => r.Id == id));

    public void Add(StockReservation aggregate) => _reservations.Add(aggregate);

    public void Update(StockReservation aggregate)
    {
        // Reference semantics: the instance is already in the list.
    }

    public Task<IReadOnlyCollection<StockReservation>> ListByOrderAsync(OrderRef orderRef, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<StockReservation>>(_reservations.Where(r => r.OrderRef == orderRef).ToList());

    public Task<IReadOnlyCollection<StockReservation>> ListOutstandingAsync(
        Sku sku, WarehouseCode warehouse, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<StockReservation>>(_reservations
            .Where(r => r.Sku == sku && r.Warehouse == warehouse &&
                        r.Status is ReservationStatus.Open or ReservationStatus.PartiallyAllocated)
            .ToList());
}

/// <summary>Collects appended movements so tests can assert how many ledger entries were written.</summary>
internal sealed class FakeStockLedger : IStockLedger
{
    private readonly List<StockMovement> _movements = [];

    public IReadOnlyList<StockMovement> Movements => _movements;

    public void Append(StockMovement movement) => _movements.Add(movement);
}

/// <summary>In-memory <see cref="IProductSnapshotRepository"/> for the put-away compatibility check.</summary>
internal sealed class FakeProductSnapshotRepository : IProductSnapshotRepository
{
    private readonly List<ProductSnapshot> _snapshots = [];

    public IReadOnlyList<ProductSnapshot> All => _snapshots;

    public void Seed(params ProductSnapshot[] snapshots) => _snapshots.AddRange(snapshots);

    public Task<ProductSnapshot?> FindAsync(Sku sku, CancellationToken cancellationToken = default) =>
        Task.FromResult(_snapshots.SingleOrDefault(s => s.Sku == sku));

    public void Add(ProductSnapshot snapshot) => _snapshots.Add(snapshot);

    public void Update(ProductSnapshot snapshot)
    {
        // Reference semantics: the instance is already in the list.
    }
}

/// <summary>In-memory <see cref="ILocationSnapshotRepository"/> for the location-projection consumer.</summary>
internal sealed class FakeLocationSnapshotRepository : ILocationSnapshotRepository
{
    private readonly List<LocationSnapshot> _snapshots = [];

    public IReadOnlyList<LocationSnapshot> All => _snapshots;

    public void Seed(params LocationSnapshot[] snapshots) => _snapshots.AddRange(snapshots);

    public Task<LocationSnapshot?> FindAsync(LocationCode code, CancellationToken cancellationToken = default) =>
        Task.FromResult(_snapshots.SingleOrDefault(s => s.Code == code));

    public Task<IReadOnlyCollection<LocationSnapshot>> ListByRoomAsync(
        WarehouseCode warehouse, string room, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<LocationSnapshot>>(
            _snapshots.Where(s => s.Warehouse == warehouse && s.Room == room).ToList());

    public void Add(LocationSnapshot snapshot) => _snapshots.Add(snapshot);

    public void Update(LocationSnapshot snapshot)
    {
        // Reference semantics: the instance is already in the list.
    }
}
