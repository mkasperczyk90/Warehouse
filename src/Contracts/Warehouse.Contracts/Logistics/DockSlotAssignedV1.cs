namespace Warehouse.Contracts.Logistics;

/// <summary>
/// Integration event: a dock + time window was booked for an announced delivery (UC-01). Published
/// by Logistics through the transactional outbox right after <c>InboundDelivery.AssignDockSlot</c>;
/// feeds the delivery timeline in Bronze (docs/BI_Plan.md §5.3 —
/// <c>BronzeInboundDelivery.dock_code</c>/<c>dock_slot_at</c>).
///
/// <b>Additive-only.</b> This is the V1 wire shape and must never change — a new field ships as
/// <c>DockSlotAssignedV2</c>, never as an edit here. Primitives only.
/// </summary>
public sealed record DockSlotAssignedV1(
    Guid DeliveryId,
    string WarehouseCode,
    string DockCode,
    DateTimeOffset SlotFrom,
    DateTimeOffset SlotTo,
    DateTimeOffset OccurredAt);
