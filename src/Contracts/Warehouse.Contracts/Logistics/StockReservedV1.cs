namespace Warehouse.Contracts.Logistics;

/// <summary>
/// Integration event: Inventory reserved stock for an outbound order (UC-09; the reply to
/// <see cref="OutboundOrderPlacedV1"/>). <see cref="Fully"/> is false when only part of the order
/// could be promised. Consumed by Logistics to move the order to Reserved / PartiallyReserved.
///
/// <b>Additive-only.</b> V1 wire shape — a new field ships as <c>StockReservedV2</c>. Primitives only.
/// </summary>
public sealed record StockReservedV1(
    Guid OrderId,
    bool Fully,
    DateTimeOffset OccurredAt);
