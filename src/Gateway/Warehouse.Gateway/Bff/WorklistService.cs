namespace Warehouse.Gateway.Bff;

/// <summary>
/// The worklist BFF: fans out to the Inventory and Logistics read models (resolved by Aspire service
/// discovery, with the standard resilience handler from <c>ServiceDefaults</c>), forwards the active
/// warehouse, and projects the results with <see cref="WorklistMapper"/>. Best-effort — a failing source
/// leaves its section empty rather than blanking the whole landing.
/// </summary>
public sealed class WorklistService(BffFetch fetch)
{
    public async Task<WorklistDto> BuildAsync(string? warehouseId, CancellationToken cancellationToken)
    {
        var warehousing = fetch.Client(BffClients.Warehousing, warehouseId);
        var logistics = fetch.Client(BffClients.Logistics, warehouseId);

        var qc = fetch.GetListAsync<QcBatchView>(warehousing, "inventory/qc/batches", cancellationToken);
        var stock = fetch.GetListAsync<StockRowView>(warehousing, "inventory/stock/rows", cancellationToken);
        var stocktakes = fetch.GetListAsync<StocktakeItemView>(warehousing, "inventory/stocktake", cancellationToken);
        var orders = fetch.GetListAsync<OrderSummaryView>(logistics, "logistics/orders", cancellationToken);
        var deliveries = fetch.GetListAsync<DeliverySummaryView>(logistics, "logistics/deliveries", cancellationToken);

        await Task.WhenAll(qc, stock, stocktakes, orders, deliveries);

        return WorklistMapper.Build(
            qc.Result, stock.Result, orders.Result, deliveries.Result, stocktakes.Result,
            DateOnly.FromDateTime(DateTime.UtcNow));
    }
}
