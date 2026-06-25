namespace Warehouse.Contracts.Logistics;

/// <summary>
/// Integration event: an outbound order was placed (UC-09). Published by Logistics through the
/// transactional outbox after the <c>OutboundOrder</c> is created, and consumed by Inventory to make
/// a soft <c>StockReservation</c> against available-to-promise for each line.
///
/// <b>Additive-only.</b> V1 wire shape — a new field ships as <c>OutboundOrderPlacedV2</c>. Primitives only.
/// </summary>
public sealed record OutboundOrderPlacedV1(
    Guid OrderId,
    string CustomerRoleId,
    string WarehouseCode,
    IReadOnlyList<OutboundOrderLineV1> Lines,
    DateTimeOffset OccurredAt);

/// <summary>One ordered line (see <see cref="OutboundOrderPlacedV1"/>).</summary>
public sealed record OutboundOrderLineV1(int LineNo, string Sku, decimal Quantity, string Unit);
