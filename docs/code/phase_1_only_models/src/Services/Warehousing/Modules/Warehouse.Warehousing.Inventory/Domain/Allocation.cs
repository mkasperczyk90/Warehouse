using Warehouse.SharedKernel.Domain;
using Warehouse.SharedKernel.ValueObjects;

namespace Warehouse.Warehousing.Inventory.Domain;

/// <summary>
/// Moment-Interval archetype (🟨): a HARD allocation — a quantity of a concrete StockItem
/// (batch + location) pinned to an order, created at wave/pick time. It traces back to the
/// soft <see cref="StockReservation"/> made when the order arrived. Lives inside the StockItem
/// aggregate; the invariant "allocated ≤ on hand" is enforced by the aggregate root.
/// </summary>
public sealed class Allocation : Entity<AllocationId>
{
    internal Allocation(AllocationId id, StockReservationId reservationId, OrderRef orderRef, Quantity quantity)
        : base(id)
    {
        ReservationId = reservationId;
        OrderRef = orderRef;
        Quantity = quantity;
        Status = AllocationStatus.Active;
    }

    private Allocation()
    {
    }

    /// <summary>The soft reservation this hard allocation fulfills.</summary>
    public StockReservationId ReservationId { get; private set; }

    public OrderRef OrderRef { get; private set; } = null!;

    public Quantity Quantity { get; private set; } = null!;

    public AllocationStatus Status { get; private set; }

    internal void ReduceBy(Quantity picked) => Quantity = Quantity.Subtract(picked);

    internal void MarkFulfilled() => Status = AllocationStatus.Fulfilled;

    internal void MarkReleased() => Status = AllocationStatus.Released;
}
