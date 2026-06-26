namespace Warehouse.Gateway.Bff;

/// <summary>A global-search hit — the desk's "where is X" (matches the admin's `SearchResult` model).
/// <see cref="Type"/> is one of product / stock / asn / order / shipment / location.</summary>
public sealed record SearchResultDto(string Type, string RefId, string Label, string Sublabel);

// --- Input slices the search BFF filters over (case-insensitive deserialization) --------------------

internal sealed record ProductView(string Sku, string Name);

internal sealed record SearchStockView(string Id, string Product, string Sku, string Batch, string Location);

internal sealed record LocationView(string Code, string Room, string Warehouse);

internal sealed record DispatchColumnView(IReadOnlyList<DispatchShipmentView> Shipments);

internal sealed record DispatchShipmentView(string Id, string Customer);
