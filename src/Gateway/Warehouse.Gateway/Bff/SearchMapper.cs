namespace Warehouse.Gateway.Bff;

/// <summary>
/// Pure projection of the lists the search BFF fetched into ranked global-search hits across products,
/// stock, inbound (ASN), orders and locations. (Shipments join once the dispatch read model lands.) No
/// I/O, so the match + ordering rules are unit-testable. Capped so the desk's command bar stays snappy.
/// </summary>
internal static class SearchMapper
{
    private const int MaxHits = 8;

    public static IReadOnlyList<SearchResultDto> Build(
        string query,
        IReadOnlyList<ProductView> products,
        IReadOnlyList<SearchStockView> stock,
        IReadOnlyList<DeliverySummaryView> deliveries,
        IReadOnlyList<OrderSummaryView> orders,
        IReadOnlyList<DispatchShipmentView> shipments,
        IReadOnlyList<LocationView> locations)
    {
        var q = query.Trim();
        if (q.Length == 0)
        {
            return [];
        }

        bool Hit(params string?[] fields) =>
            fields.Any(f => f is not null && f.Contains(q, StringComparison.OrdinalIgnoreCase));

        var hits = new List<SearchResultDto>();

        foreach (var p in products.Where(p => Hit(p.Sku, p.Name)))
        {
            hits.Add(new SearchResultDto("product", p.Sku, p.Name, $"SKU {p.Sku}"));
        }

        foreach (var s in stock.Where(s => Hit(s.Sku, s.Product, s.Batch, s.Location)))
        {
            hits.Add(new SearchResultDto("stock", s.Id, $"{s.Product} · {s.Batch}", $"{s.Location} · {s.Sku}"));
        }

        foreach (var d in deliveries.Where(d => Hit(d.Id, d.WarehouseCode)))
        {
            hits.Add(new SearchResultDto("asn", d.Id, d.Id, d.WarehouseCode));
        }

        foreach (var o in orders.Where(o => Hit(o.Id, o.WarehouseCode)))
        {
            hits.Add(new SearchResultDto("order", o.Id, o.Id, o.WarehouseCode));
        }

        foreach (var s in shipments.Where(s => Hit(s.Id, s.Customer)))
        {
            hits.Add(new SearchResultDto("shipment", s.Id, s.Id, s.Customer));
        }

        foreach (var l in locations.Where(l => Hit(l.Code)))
        {
            hits.Add(new SearchResultDto("location", l.Code, l.Code, l.Room));
        }

        return hits.Count > MaxHits ? hits.GetRange(0, MaxHits) : hits;
    }
}
