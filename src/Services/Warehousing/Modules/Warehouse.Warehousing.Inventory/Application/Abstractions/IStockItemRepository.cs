using Warehouse.SharedKernel.Application;
using Warehouse.Warehousing.Inventory.Domain;

namespace Warehouse.Warehousing.Inventory.Application.Abstractions;

/// <summary>Persistence port for the <see cref="StockItem"/> aggregate (SKU + batch + location).</summary>
public interface IStockItemRepository : IRepository<StockItem, StockItemId>
{
    /// <summary>The single stock item for a SKU/batch at a location, or <c>null</c> if none exists yet.</summary>
    Task<StockItem?> GetAtAsync(Sku sku, BatchNumber? batch, LocationCode location, CancellationToken cancellationToken = default);

    /// <summary>
    /// Every stock item of a SKU in a warehouse — the application sums these into available-to-promise
    /// before calling <c>ReservationService</c> (ATP spans all locations, so no aggregate can hold it).
    /// </summary>
    Task<IReadOnlyCollection<StockItem>> ListBySkuAsync(Sku sku, WarehouseCode warehouse, CancellationToken cancellationToken = default);

    /// <summary>Every stock item currently at a location — used to drive put-away off the dock buffer.</summary>
    Task<IReadOnlyCollection<StockItem>> ListAtAsync(LocationCode location, CancellationToken cancellationToken = default);
}
