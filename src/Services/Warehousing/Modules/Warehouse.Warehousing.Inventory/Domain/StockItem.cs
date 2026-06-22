using Warehouse.SharedKernel.Domain;
using Warehouse.SharedKernel.ValueObjects;
using Warehouse.Warehousing.Inventory.Domain.Events;

namespace Warehouse.Warehousing.Inventory.Domain;

/// <summary>
/// The Inventory archetype entry and our core aggregate: quantity of one SKU (and batch)
/// at one location. Deliberately small — every scan gun confirmation is a transaction on
/// a single StockItem, so there are no hot rows.
///
/// Every behavior that changes <see cref="OnHand"/> RETURNS the <see cref="StockMovement"/>
/// to persist — stock cannot change without a ledger entry; the application layer saves
/// both in one transaction. Location-to-location moves go through
/// <see cref="Services.StockTransferService"/> (one movement, two stock items).
///
/// <b>Allocation, not reservation.</b> Orders take a soft <see cref="StockReservation"/> at
/// order time (SKU-level, no pallet pinned). Only at wave/pick time is a hard
/// <see cref="Allocation"/> pinned to this concrete StockItem — via
/// <see cref="Services.AllocationPolicy"/>, which re-checks batch quality at that moment.
/// Invariants here: on-hand ≥ 0, allocated ≤ on-hand, no allocation on blocked stock.
/// Cross-item invariants (location capacity) live in <see cref="Services.PutAwayPolicy"/>
/// plus a database constraint.
/// </summary>
public sealed class StockItem : AggregateRoot<StockItemId>
{
    private static readonly MovementType[] InboundTypes = [MovementType.GoodsReceipt, MovementType.TransferIn];

    private readonly List<Allocation> _allocations = [];

    private StockItem(StockItemId id, Sku sku, BatchNumber? batch, LocationCode location, UnitOfMeasure unit)
        : base(id)
    {
        Sku = sku;
        Batch = batch;
        Location = location;
        OnHand = Quantity.Zero(unit);
        Allocated = Quantity.Zero(unit);
        Status = StockStatus.Available;
    }

    private StockItem()
    {
    }

    public Sku Sku { get; private set; } = null!;

    /// <summary>Null for products that are not batch-tracked.</summary>
    public BatchNumber? Batch { get; private set; }

    public LocationCode Location { get; private set; } = null!;

    public Quantity OnHand { get; private set; } = null!;

    /// <summary>Quantity hard-allocated to orders (pinned here, awaiting pick).</summary>
    public Quantity Allocated { get; private set; } = null!;

    public StockStatus Status { get; private set; }

    public IReadOnlyCollection<Allocation> Allocations => _allocations.AsReadOnly();

    /// <summary>Free to allocate right now: on hand minus already allocated (zero if not Available).</summary>
    public Quantity Available => Status == StockStatus.Available ? OnHand.Subtract(Allocated) : Quantity.Zero(OnHand.Unit);

    public static StockItem CreateAt(LocationCode location, Sku sku, BatchNumber? batch, UnitOfMeasure unit)
    {
        ArgumentNullException.ThrowIfNull(location);
        ArgumentNullException.ThrowIfNull(sku);
        ArgumentNullException.ThrowIfNull(unit);
        return new StockItem(StockItemId.New(), sku, batch, location, unit);
    }

    /// <summary>
    /// Stock enters the warehouse at this location (goods receipt into the dock buffer,
    /// or an inter-warehouse transfer arriving). Put-away/moves use StockTransferService.
    /// </summary>
    public StockMovement Receive(Quantity quantity, MovementType type, string performedBy, string? reason = null)
    {
        ArgumentNullException.ThrowIfNull(quantity);
        if (!InboundTypes.Contains(type))
        {
            throw new DomainException(
                "movement_type_invalid",
                $"Receive accepts only {string.Join('/', InboundTypes)}, got {type}. Use StockTransferService for moves.");
        }

        OnHand = OnHand.Add(quantity);
        Raise(new StockReceived(Id, Sku, Location, quantity, DateTimeOffset.UtcNow));
        return StockMovement.Record(type, Sku, Batch, from: null, to: Location, quantity, performedBy, reason);
    }

    /// <summary>
    /// Pins a hard allocation to this stock for a soft reservation. Produces no movement
    /// (stock doesn't move until picked). Batch quality is re-checked here at wave/pick time —
    /// call via <see cref="Services.AllocationPolicy"/>, never directly, for batch-tracked stock.
    /// </summary>
    public Allocation Allocate(Quantity quantity, OrderRef orderRef, StockReservationId reservationId)
    {
        ArgumentNullException.ThrowIfNull(quantity);
        ArgumentNullException.ThrowIfNull(orderRef);

        if (Status != StockStatus.Available)
        {
            throw new DomainException(
                "stock_not_available",
                $"Stock {Sku} at {Location} is {Status} and cannot be allocated.");
        }

        if (!Available.IsGreaterThanOrEqualTo(quantity))
        {
            throw new DomainException(
                "stock_insufficient",
                $"Cannot allocate {quantity} of {Sku} at {Location}: only {Available} available.");
        }

        var allocation = new Allocation(AllocationId.New(), reservationId, orderRef, quantity);
        _allocations.Add(allocation);
        Allocated = Allocated.Add(quantity);
        Raise(new StockAllocated(Id, allocation.Id, reservationId, orderRef, quantity, DateTimeOffset.UtcNow));
        return allocation;
    }

    public void ReleaseAllocation(AllocationId allocationId)
    {
        var allocation = GetActiveAllocation(allocationId);
        Allocated = Allocated.Subtract(allocation.Quantity);
        allocation.MarkReleased();
        Raise(new AllocationReleased(Id, allocation.Id, allocation.OrderRef, allocation.Quantity, DateTimeOffset.UtcNow));
    }

    /// <summary>Physical pick against an allocation; partial picks shrink the allocation.</summary>
    public StockMovement Pick(AllocationId allocationId, Quantity quantity, string performedBy)
    {
        ArgumentNullException.ThrowIfNull(quantity);
        var allocation = GetActiveAllocation(allocationId);

        if (!allocation.Quantity.IsGreaterThanOrEqualTo(quantity))
        {
            throw new DomainException(
                "pick_exceeds_allocation",
                $"Cannot pick {quantity}: allocation {allocationId} holds only {allocation.Quantity}.");
        }

        OnHand = OnHand.Subtract(quantity);
        Allocated = Allocated.Subtract(quantity);
        allocation.ReduceBy(quantity);
        if (allocation.Quantity.IsZero)
        {
            allocation.MarkFulfilled();
        }

        Raise(new StockPicked(Id, allocationId, Sku, Location, quantity, DateTimeOffset.UtcNow));
        return StockMovement.Record(
            MovementType.Pick, Sku, Batch, from: Location, to: null, quantity, performedBy,
            reason: $"Order {allocation.OrderRef}");
    }

    /// <summary>Manual correction (damage, loss, stocktake difference) — always with a reason.</summary>
    public StockMovement AdjustTo(Quantity newOnHand, string reason, string performedBy)
    {
        ArgumentNullException.ThrowIfNull(newOnHand);
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        if (newOnHand == OnHand)
        {
            throw new DomainException(
                "adjustment_no_change",
                $"Stock {Sku} at {Location} already has {OnHand} on hand.");
        }

        if (!newOnHand.IsGreaterThanOrEqualTo(Allocated))
        {
            throw new DomainException(
                "adjustment_below_allocated",
                $"Cannot adjust {Sku} at {Location} to {newOnHand}: {Allocated} is allocated — release allocations first.");
        }

        var before = OnHand;
        OnHand = newOnHand;
        Raise(new StockAdjusted(Id, Sku, Location, before, newOnHand, reason, DateTimeOffset.UtcNow));

        return newOnHand.IsGreaterThanOrEqualTo(before)
            ? StockMovement.Record(
                MovementType.AdjustmentIn, Sku, Batch, from: null, to: Location,
                newOnHand.Subtract(before), performedBy, reason)
            : StockMovement.Record(
                MovementType.AdjustmentOut, Sku, Batch, from: Location, to: null,
                before.Subtract(newOnHand), performedBy, reason);
    }

    public void MarkQuarantine() => Status = StockStatus.Quarantine;

    public void MarkBlocked()
    {
        if (_allocations.Any(a => a.Status == AllocationStatus.Active))
        {
            throw new DomainException(
                "block_with_active_allocations",
                $"Stock {Sku} at {Location} has active allocations — release them before blocking.");
        }

        Status = StockStatus.Blocked;
    }

    public void MarkAvailable() => Status = StockStatus.Available;

    /// <summary>Outgoing half of a transfer; only StockTransferService may call it.</summary>
    internal void TransferOut(Quantity quantity)
    {
        var unallocated = OnHand.Subtract(Allocated);
        if (!unallocated.IsGreaterThanOrEqualTo(quantity))
        {
            throw new DomainException(
                "transfer_exceeds_unallocated",
                $"Cannot move {quantity} of {Sku} from {Location}: only {unallocated} is unallocated.");
        }

        OnHand = OnHand.Subtract(quantity);
    }

    /// <summary>Incoming half of a transfer; only StockTransferService may call it.</summary>
    internal void TransferIn(Quantity quantity) => OnHand = OnHand.Add(quantity);

    private Allocation GetActiveAllocation(AllocationId allocationId) =>
        _allocations.SingleOrDefault(a => a.Id == allocationId && a.Status == AllocationStatus.Active)
        ?? throw new DomainException("allocation_not_found", $"No active allocation {allocationId} on stock item {Id}.");
}
