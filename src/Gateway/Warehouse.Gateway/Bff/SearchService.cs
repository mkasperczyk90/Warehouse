namespace Warehouse.Gateway.Bff;

/// <summary>
/// The global-search BFF: fans out to products (MasterData), stock + locations (Warehousing) and
/// inbound/orders (Logistics), then filters + ranks with <see cref="SearchMapper"/>. Best-effort, scoped
/// to the active warehouse. (Shipment hits join once the dispatch read model lands.)
/// </summary>
public sealed class SearchService(BffFetch fetch)
{
    public async Task<IReadOnlyList<SearchResultDto>> SearchAsync(
        string query, string? warehouseId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        var warehousing = fetch.Client(BffClients.Warehousing, warehouseId);
        var logistics = fetch.Client(BffClients.Logistics, warehouseId);
        var masterData = fetch.Client(BffClients.MasterData, warehouseId);

        var products = fetch.GetListAsync<ProductView>(masterData, "catalog/products", cancellationToken);
        var stock = fetch.GetListAsync<SearchStockView>(warehousing, "inventory/stock/rows", cancellationToken);
        var locations = fetch.GetListAsync<LocationView>(warehousing, "topology/locations", cancellationToken);
        var deliveries = fetch.GetListAsync<DeliverySummaryView>(logistics, "logistics/deliveries", cancellationToken);
        var orders = fetch.GetListAsync<OrderSummaryView>(logistics, "logistics/orders", cancellationToken);
        var board = fetch.GetListAsync<DispatchColumnView>(logistics, "dispatch/board", cancellationToken);

        await Task.WhenAll(products, stock, locations, deliveries, orders, board);

        var shipments = board.Result.SelectMany(c => c.Shipments).ToList();
        return SearchMapper.Build(
            query, products.Result, stock.Result, deliveries.Result, orders.Result, shipments, locations.Result);
    }
}
