namespace Warehouse.Contracts.Logistics;

/// <summary>
/// Integration event: an order's shipment was collected by the carrier (UC-12). Published by
/// Logistics; consumed by Inventory to deduct the dispatched stock from the ledger.
///
/// <b>Additive-only.</b> V1 wire shape — a new field ships as <c>ShipmentDispatchedV2</c>. Primitives only.
/// </summary>
public sealed record ShipmentDispatchedV1(
    Guid OrderId,
    Guid ShipmentId,
    string WarehouseCode,
    string CarrierRoleId,
    string? TrackingNumber,
    DateTimeOffset OccurredAt);
