namespace Warehouse.Contracts.Logistics;

/// <summary>
/// Integration event: an order was released to the picking floor (UC-10, wave release). Published by
/// Logistics; consumed by Inventory to turn the order's soft reservations into hard allocations
/// against concrete stock (FEFO) and reply with the planned picks.
///
/// <b>Additive-only.</b> V1 wire shape — a new field ships as <c>PickingReleasedV2</c>. Primitives only.
/// </summary>
public sealed record PickingReleasedV1(
    Guid OrderId,
    string WarehouseCode,
    DateTimeOffset OccurredAt);
