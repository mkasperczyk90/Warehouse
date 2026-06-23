using Warehouse.Logistics.Core.Application.Abstractions;
using Warehouse.Logistics.Core.Domain;
using Warehouse.Logistics.Core.Domain.Replicas;
using Warehouse.SharedKernel.Application;

namespace Warehouse.Logistics.Tests.TestDoubles;

/// <summary>Counts commits so handler tests can assert the unit of work was flushed once.</summary>
internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveCount { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveCount++;
        return Task.FromResult(1);
    }
}

/// <summary>In-memory <see cref="IOutboundOrderRepository"/>. Aggregates are mutated by reference, so
/// <see cref="Add"/>/<see cref="Update"/> only need to register the instance.</summary>
internal sealed class FakeOutboundOrderRepository : IOutboundOrderRepository
{
    private readonly Dictionary<OrderId, OutboundOrder> _store = [];

    public IReadOnlyCollection<OutboundOrder> Saved => _store.Values;

    public void Seed(OutboundOrder order) => _store[order.Id] = order;

    public Task<OutboundOrder?> GetByIdAsync(OrderId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.GetValueOrDefault(id));

    public void Add(OutboundOrder aggregate) => _store[aggregate.Id] = aggregate;

    public void Update(OutboundOrder aggregate) => _store[aggregate.Id] = aggregate;

    public Task<IReadOnlyCollection<OutboundOrder>> ListByStatusAsync(
        OrderStatus status, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<OutboundOrder>>(
            _store.Values.Where(o => o.Status == status).ToList());
}

internal sealed class FakePickListRepository : IPickListRepository
{
    private readonly Dictionary<PickListId, PickList> _store = [];

    public void Seed(PickList pickList) => _store[pickList.Id] = pickList;

    public Task<PickList?> GetByIdAsync(PickListId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.GetValueOrDefault(id));

    public void Add(PickList aggregate) => _store[aggregate.Id] = aggregate;

    public void Update(PickList aggregate) => _store[aggregate.Id] = aggregate;

    public Task<PickList?> GetByOrderAsync(OrderId orderId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.Values.SingleOrDefault(p => p.OrderId == orderId));
}

internal sealed class FakeShipmentRepository : IShipmentRepository
{
    private readonly Dictionary<ShipmentId, Shipment> _store = [];

    public IReadOnlyCollection<Shipment> Saved => _store.Values;

    public Task<Shipment?> GetByIdAsync(ShipmentId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.GetValueOrDefault(id));

    public void Add(Shipment aggregate) => _store[aggregate.Id] = aggregate;

    public void Update(Shipment aggregate) => _store[aggregate.Id] = aggregate;

    public Task<Shipment?> GetByOrderAsync(OrderId orderId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.Values.SingleOrDefault(s => s.OrderId == orderId));
}

internal sealed class FakeInboundDeliveryRepository : IInboundDeliveryRepository
{
    private readonly Dictionary<DeliveryId, InboundDelivery> _store = [];

    public IReadOnlyCollection<InboundDelivery> Saved => _store.Values;

    /// <summary>Slots the conflict probe will return, regardless of the requested window.</summary>
    public List<DockSlot> BookedSlots { get; } = [];

    public void Seed(InboundDelivery delivery) => _store[delivery.Id] = delivery;

    public Task<InboundDelivery?> GetByIdAsync(DeliveryId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.GetValueOrDefault(id));

    public void Add(InboundDelivery aggregate) => _store[aggregate.Id] = aggregate;

    public void Update(InboundDelivery aggregate) => _store[aggregate.Id] = aggregate;

    public Task<IReadOnlyCollection<DockSlot>> ListBookedSlotsAsync(
        string dockCode, DateTimeOffset windowStart, DateTimeOffset windowEnd, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<DockSlot>>(
            BookedSlots.Where(s => s.DockCode == dockCode).ToList());
}

/// <summary>In-memory Catalog replica. Codes added via <see cref="Seed"/> are "known"; everything
/// else comes back as unknown so the announce/order handlers reject it.</summary>
internal sealed class FakeCatalogProductReplica : ICatalogProductReplica
{
    private readonly Dictionary<string, CatalogProductSnapshot> _store = new(StringComparer.OrdinalIgnoreCase);

    public void Seed(params string[] codes)
    {
        foreach (var code in codes)
        {
            var product = ProductCode.Of(code);
            _store[product.Value] = new CatalogProductSnapshot(product, "pcs", isBatchTracked: false, DateTimeOffset.UtcNow);
        }
    }

    public Task<CatalogProductSnapshot?> FindAsync(ProductCode code, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.GetValueOrDefault(code.Value));

    public Task<IReadOnlyCollection<ProductCode>> FindUnknownAsync(
        IReadOnlyCollection<ProductCode> codes, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<ProductCode>>(
            codes.Where(c => !_store.ContainsKey(c.Value)).ToList());

    public void Add(CatalogProductSnapshot snapshot) => _store[snapshot.Code.Value] = snapshot;

    public void Update(CatalogProductSnapshot snapshot) => _store[snapshot.Code.Value] = snapshot;
}
