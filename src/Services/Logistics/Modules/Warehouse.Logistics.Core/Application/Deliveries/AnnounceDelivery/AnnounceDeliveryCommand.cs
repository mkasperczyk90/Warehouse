namespace Warehouse.Logistics.Core.Application.Deliveries.AnnounceDelivery;

/// <summary>UC-01 — announce an inbound delivery (ASN): supplier, target warehouse, planned date, lines.</summary>
public sealed record AnnounceDeliveryCommand(
    string SupplierRoleId,
    string WarehouseCode,
    DateTimeOffset PlannedAt,
    IReadOnlyList<AnnounceDeliveryLine> Lines);

/// <summary>One announced line. <see cref="PackFactor"/>/<see cref="PackUnit"/> carry an optional
/// per-delivery pack (1 pack unit = factor base units); omit them to use the catalog default.</summary>
public sealed record AnnounceDeliveryLine(
    string ProductCode,
    decimal Quantity,
    string Unit,
    decimal? PackFactor = null,
    string? PackUnit = null);
