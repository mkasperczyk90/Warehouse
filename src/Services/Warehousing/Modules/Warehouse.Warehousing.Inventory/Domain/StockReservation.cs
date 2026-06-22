using Warehouse.SharedKernel.Domain;
using Warehouse.SharedKernel.ValueObjects;
using Warehouse.Warehousing.Inventory.Domain.Events;

namespace Warehouse.Warehousing.Inventory.Domain;

/// <summary>
/// Moment-Interval archetype (🟨): a SOFT reservation — a promise of a quantity of one SKU
/// in one warehouse, made when an order arrives. It protects available-to-promise without
/// pinning any physical pallet, so the goods stay free to move, be QC-checked or re-slotted
/// until the order is released to the floor. Hard <see cref="Allocation"/>s against concrete
/// StockItems (FEFO, quality re-checked) are created later at wave/pick time and reported
/// back here via <see cref="RecordAllocation"/>.
/// </summary>
public sealed class StockReservation : AggregateRoot<StockReservationId>
{
    private StockReservation(StockReservationId id, Sku sku, WarehouseCode warehouse, OrderRef orderRef, Quantity quantity)
        : base(id)
    {
        Sku = sku;
        Warehouse = warehouse;
        OrderRef = orderRef;
        Quantity = quantity;
        Allocated = Quantity.Zero(quantity.Unit);
        Status = ReservationStatus.Open;
    }

    private StockReservation()
    {
    }

    public Sku Sku { get; private set; } = null!;

    public WarehouseCode Warehouse { get; private set; } = null!;

    public OrderRef OrderRef { get; private set; } = null!;

    /// <summary>The promised quantity.</summary>
    public Quantity Quantity { get; private set; } = null!;

    /// <summary>How much has been hard-allocated to concrete stock so far.</summary>
    public Quantity Allocated { get; private set; } = null!;

    public ReservationStatus Status { get; private set; }

    public Quantity Outstanding => Quantity.Subtract(Allocated);

    /// <summary>
    /// Created by <see cref="Services.ReservationService"/>, which checks available-to-promise
    /// first. Use the service, not this factory, so the ATP gate is never bypassed.
    /// </summary>
    internal static StockReservation Open(Sku sku, WarehouseCode warehouse, OrderRef orderRef, Quantity quantity)
    {
        ArgumentNullException.ThrowIfNull(sku);
        ArgumentNullException.ThrowIfNull(warehouse);
        ArgumentNullException.ThrowIfNull(orderRef);
        ArgumentNullException.ThrowIfNull(quantity);
        if (quantity.IsZero)
        {
            throw new DomainException("reservation_quantity_zero", "A reservation must promise a positive quantity.");
        }

        var reservation = new StockReservation(StockReservationId.New(), sku, warehouse, orderRef, quantity);
        reservation.Raise(new StockReserved(reservation.Id, sku, warehouse, orderRef, quantity, DateTimeOffset.UtcNow));
        return reservation;
    }

    /// <summary>Records that a hard allocation was made against concrete stock for this reservation.</summary>
    public void RecordAllocation(Quantity quantity)
    {
        ArgumentNullException.ThrowIfNull(quantity);
        if (Status is not (ReservationStatus.Open or ReservationStatus.PartiallyAllocated))
        {
            throw new DomainException(
                "reservation_not_allocatable",
                $"Reservation {Id} is {Status} and can no longer be allocated.");
        }

        var newAllocated = Allocated.Add(quantity);
        if (newAllocated.Amount > Quantity.Amount)
        {
            throw new DomainException(
                "reservation_over_allocated",
                $"Allocating {quantity} would exceed reservation {Id} ({Quantity} promised, {Allocated} already allocated).");
        }

        Allocated = newAllocated;
        Status = Outstanding.IsZero ? ReservationStatus.Allocated : ReservationStatus.PartiallyAllocated;
    }

    public void Release()
    {
        if (Status == ReservationStatus.Allocated)
        {
            throw new DomainException("reservation_fully_allocated", $"Reservation {Id} is fully allocated; release the allocations instead.");
        }

        Status = ReservationStatus.Released;
        Raise(new ReservationReleased(Id, Sku, Warehouse, OrderRef, Outstanding, DateTimeOffset.UtcNow));
    }

    public void Cancel() => Status = ReservationStatus.Cancelled;
}
