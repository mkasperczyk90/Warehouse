namespace Warehouse.Contracts.Logistics;

/// <summary>
/// Integration event: an outbound order was cancelled. Published by Logistics; consumed by Inventory
/// to release the order's soft reservations (freeing available-to-promise).
///
/// <b>Additive-only.</b> V1 wire shape — a new field ships as <c>OutboundOrderCancelledV2</c>. Primitives only.
/// </summary>
public sealed record OutboundOrderCancelledV1(
    Guid OrderId,
    DateTimeOffset OccurredAt);
