namespace Warehouse.Contracts.Logistics;

/// <summary>
/// Integration event: a goods receipt was confirmed for an inbound delivery (UC-02). Published by
/// Logistics through the transactional outbox after the <c>InboundDelivery</c> reaches
/// <c>Received</c>, and consumed by Inventory to bring the received quantities on stock in the
/// warehouse's dock buffer.
///
/// <b>Additive-only.</b> This is the V1 wire shape and must never change — a new field ships as
/// <c>GoodsReceiptConfirmedV2</c>, never as an edit here. Primitives only: <c>Discrepancy</c> travels
/// as the enum <i>name</i> so the contract carries no domain types.
/// </summary>
public sealed record GoodsReceiptConfirmedV1(
    Guid DeliveryId,
    string WarehouseCode,
    IReadOnlyList<GoodsReceiptLineV1> Lines,
    DateTimeOffset OccurredAt);

/// <summary>One received line of a goods receipt (see <see cref="GoodsReceiptConfirmedV1"/>).</summary>
public sealed record GoodsReceiptLineV1(
    int LineNo,
    string Sku,
    decimal Quantity,
    string Unit,
    string? BatchNumber,
    DateOnly? ExpiryDate,
    string Discrepancy);
