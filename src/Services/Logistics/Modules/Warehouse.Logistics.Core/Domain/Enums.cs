namespace Warehouse.Logistics.Core.Domain;

public enum DeliveryStatus
{
    Announced = 0,
    Arrived = 1,
    Receiving = 2,
    Received = 3,
    PutAwayInProgress = 4,
    Completed = 5,
    Cancelled = 6,
}

public enum OrderStatus
{
    Created = 0,
    PartiallyReserved = 1,
    Reserved = 2,
    Picking = 3,
    Packed = 4,
    Dispatched = 5,
    Cancelled = 6,
}

public enum DiscrepancyType
{
    None = 0,
    Shortage = 1,
    Overage = 2,
    Damaged = 3,
}

/// <summary>The coordinator's call on a partially-reserved order (UC-09): ship what's available now
/// (split, remainder backordered) or wait for stock to cover the whole order (hold).</summary>
public enum PartialDecision
{
    Split = 0,
    Hold = 1,
}

public enum PickTaskStatus
{
    Pending = 0,
    Picked = 1,

    /// <summary>Location turned out short — the task needs replanning from another location/batch.</summary>
    ShortPick = 2,
}

public enum ShipmentStatus
{
    AwaitingCarrier = 0,
    CarrierAssigned = 1,
    ReadyForPickup = 2,
    Dispatched = 3,
}
