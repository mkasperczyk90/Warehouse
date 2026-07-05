namespace Warehouse.Contracts.Logistics;

/// <summary>
/// Integration event: an inbound delivery (ASN) was announced (UC-01). Published by Logistics
/// through the transactional outbox right after <c>InboundDelivery.Announce</c>; feeds the delivery
/// timeline in Bronze (docs/BI_Plan.md §5.3 — <c>BronzeInboundDelivery.announced_at</c>).
///
/// <b>Additive-only.</b> This is the V1 wire shape and must never change — a new field ships as
/// <c>DeliveryAnnouncedV2</c>, never as an edit here. Primitives only.
/// </summary>
public sealed record DeliveryAnnouncedV1(
    Guid DeliveryId,
    string WarehouseCode,
    string SupplierRoleId,
    DateTimeOffset PlannedAt,
    DateTimeOffset OccurredAt);
