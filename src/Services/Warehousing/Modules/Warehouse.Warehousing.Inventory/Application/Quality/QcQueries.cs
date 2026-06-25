using Microsoft.EntityFrameworkCore;
using Warehouse.Warehousing.Inventory.Domain;
using Warehouse.Warehousing.Inventory.Infrastructure.Persistence;

namespace Warehouse.Warehousing.Inventory.Application.Quality;

/// <summary>
/// UC-03 read model: the QC worklist — batches held in quarantine, awaiting an inspector's release/reject.
/// Driven by the physically-held stock (<see cref="StockItem"/> in <see cref="StockStatus.Quarantine"/>),
/// grouped by batch, since a QC decision is batch-level (a blocked batch is invisible to allocation across
/// every location at once). Warehouse-scoped via the held stock's location prefix.
/// </summary>
public sealed record QcBatchDto(
    string Id,
    string Batch,
    string Product,
    string Sku,
    string FromReceipt,
    string Location,
    decimal Qty,
    string Unit,
    string Status,
    string StatusLabel);

public sealed class QcQueries(InventoryDbContext db)
{
    public async Task<IReadOnlyList<QcBatchDto>> ListAsync(
        string warehouse, CancellationToken cancellationToken = default)
    {
        var items = await db.StockItems.AsNoTracking().ToListAsync(cancellationToken);
        var batches = await db.Batches.AsNoTracking().ToListAsync(cancellationToken);
        var products = await db.ProductSnapshots.AsNoTracking().ToListAsync(cancellationToken);

        var batchByKey = batches.ToDictionary(b => (b.Sku.Value, b.Number.Value));
        var nameBySku = products.ToDictionary(p => p.Sku.Value, p => p.Name);

        return items
            .Where(i => i.Status == StockStatus.Quarantine && !i.OnHand.IsZero && i.Batch is not null)
            .Where(i => WarehouseOf(i.Location.Value) == warehouse)
            .GroupBy(i => (Sku: i.Sku.Value, Batch: i.Batch!.Value))
            .Select(g =>
            {
                batchByKey.TryGetValue(g.Key, out var batch);
                var locations = g.Select(i => i.Location.Value).Distinct().ToList();
                var location = locations.Count == 1 ? locations[0] : $"{locations[0]} +{locations.Count - 1}";
                return new QcBatchDto(
                    batch?.Id.Value.ToString() ?? g.Key.Batch,
                    g.Key.Batch,
                    nameBySku.GetValueOrDefault(g.Key.Sku, g.Key.Sku),
                    g.Key.Sku,
                    batch?.SupplierRef ?? "—",
                    location,
                    g.Sum(i => i.OnHand.Amount),
                    g.First().OnHand.Unit.Code,
                    "blocked",
                    "Quarantine");
            })
            .ToList();
    }

    private static string WarehouseOf(string location)
    {
        var dash = location.IndexOf('-', StringComparison.Ordinal);
        return dash < 0 ? location : location[..dash];
    }
}
