namespace Warehouse.Contracts.Logistics;

/// <summary>
/// Integration event: every line of a delivery's received stock has been put away into its target
/// location (UC-04). Published by Inventory once the dock buffer for the delivery is cleared, and
/// consumed by Logistics to complete the delivery (<c>PutAwayInProgress → Completed</c>).
///
/// <b>Additive-only.</b> V1 wire shape — a new field ships as <c>PutAwayCompletedV2</c>. Primitives only.
/// </summary>
public sealed record PutAwayCompletedV1(
    Guid DeliveryId,
    DateTimeOffset OccurredAt);
