namespace Warehouse.Contracts.Logistics;

/// <summary>
/// Integration event: the truck checked in at the dock for an announced delivery (UC-02). Published
/// by Logistics through the transactional outbox right after <c>InboundDelivery.RegisterArrival</c>;
/// feeds the delivery timeline in Bronze (docs/BI_Plan.md §5.3 —
/// <c>BronzeInboundDelivery.arrived_at</c>).
///
/// <b>Additive-only.</b> This is the V1 wire shape and must never change — a new field ships as
/// <c>DeliveryArrivedV2</c>, never as an edit here. Primitives only.
/// </summary>
public sealed record DeliveryArrivedV1(
    Guid DeliveryId,
    string WarehouseCode,
    DateTimeOffset OccurredAt);
