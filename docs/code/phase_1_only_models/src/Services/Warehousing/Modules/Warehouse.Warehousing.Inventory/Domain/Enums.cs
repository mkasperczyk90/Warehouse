namespace Warehouse.Warehousing.Inventory.Domain;

public enum StockStatus
{
    Available = 0,

    /// <summary>Awaiting quality inspection — invisible to allocation.</summary>
    Quarantine = 1,

    /// <summary>Blocked (QC rejection, damage) — invisible to allocation.</summary>
    Blocked = 2,
}

/// <summary>
/// Description archetype (🟦) for ledger entries. The direction of a movement follows
/// from its type and from/to locations — amounts are never signed.
/// </summary>
public enum MovementType
{
    GoodsReceipt = 0,
    PutAway = 1,
    Move = 2,
    Pick = 3,
    Dispatch = 4,
    AdjustmentIn = 5,
    AdjustmentOut = 6,
    StocktakeDifference = 7,
    TransferOut = 8,
    TransferIn = 9,
}

/// <summary>
/// Soft reservation against available-to-promise (created at order time, SKU-level).
/// </summary>
public enum ReservationStatus
{
    Open = 0,
    PartiallyAllocated = 1,
    Allocated = 2,
    Released = 3,
    Cancelled = 4,
}

/// <summary>
/// Hard allocation pinned to a concrete StockItem (created at wave/pick time via FEFO).
/// </summary>
public enum AllocationStatus
{
    Active = 0,
    Released = 1,
    Fulfilled = 2,
}

public enum QualityStatus
{
    Released = 0,
    Quarantine = 1,
    Rejected = 2,
}

public enum HandlingUnitKind
{
    Pallet = 0,
    Carton = 1,
    Tote = 2,
}

public enum HandlingUnitStatus
{
    /// <summary>Contents may still change (packing in progress).</summary>
    Open = 0,

    /// <summary>Sealed — moved and shipped as a whole.</summary>
    Closed = 1,
}

public enum StocktakeStatus
{
    Open = 0,
    Counting = 1,
    Approved = 2,
    Cancelled = 3,
}
