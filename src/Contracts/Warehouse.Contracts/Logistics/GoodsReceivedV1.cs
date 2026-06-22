namespace Warehouse.Contracts.Logistics;

/// <summary>
/// Integration event: Inventory brought a confirmed goods receipt on stock in the dock buffer
/// (the reply to <see cref="GoodsReceiptConfirmedV1"/>). Consumed by Logistics to advance the
/// delivery into put-away (UC-04).
///
/// <b>Additive-only.</b> V1 wire shape — a new field ships as <c>GoodsReceivedV2</c>. Primitives only.
/// </summary>
public sealed record GoodsReceivedV1(
    Guid DeliveryId,
    string WarehouseCode,
    string BufferLocation,
    int LineCount,
    DateTimeOffset OccurredAt);
