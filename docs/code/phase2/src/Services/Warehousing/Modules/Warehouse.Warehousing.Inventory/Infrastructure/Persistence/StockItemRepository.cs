using Microsoft.EntityFrameworkCore;
using Warehouse.Warehousing.Inventory.Application.Abstractions;
using Warehouse.Warehousing.Inventory.Domain;

namespace Warehouse.Warehousing.Inventory.Infrastructure.Persistence;

/// <summary>EF Core implementation of the stock-item persistence port. Allocations are an owned
/// collection, so they load with the aggregate.</summary>
internal sealed class StockItemRepository(InventoryDbContext context) : IStockItemRepository
{
    public Task<StockItem?> GetByIdAsync(StockItemId id, CancellationToken cancellationToken = default) =>
        context.StockItems.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public void Add(StockItem aggregate) => context.StockItems.Add(aggregate);

    public void Update(StockItem aggregate) => context.StockItems.Update(aggregate);

    public Task<StockItem?> GetAtAsync(Sku sku, BatchNumber? batch, LocationCode location, CancellationToken cancellationToken = default) =>
        batch is null
            ? context.StockItems.FirstOrDefaultAsync(
                s => s.Sku == sku && s.Location == location && s.Batch == null, cancellationToken)
            : context.StockItems.FirstOrDefaultAsync(
                s => s.Sku == sku && s.Location == location && s.Batch == batch, cancellationToken);

    public async Task<IReadOnlyCollection<StockItem>> ListBySkuAsync(
        Sku sku, WarehouseCode warehouse, CancellationToken cancellationToken = default)
    {
        // A location code is "<warehouse>-<aisle>-...", so a warehouse's stock is every item whose
        // location starts with the warehouse code. The location is a value-converted column, which
        // EF can't translate StartsWith against, so we narrow by SKU in the database and filter the
        // (small, per-SKU) result set by warehouse in memory.
        var prefix = warehouse.Value + "-";
        var items = await context.StockItems
            .Where(s => s.Sku == sku)
            .ToListAsync(cancellationToken);
        return items.Where(s => s.Location.Value.StartsWith(prefix, StringComparison.Ordinal)).ToList();
    }
}
