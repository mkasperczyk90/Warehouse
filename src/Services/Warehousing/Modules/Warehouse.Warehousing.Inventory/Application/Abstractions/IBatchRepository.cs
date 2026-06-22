using Warehouse.SharedKernel.Application;
using Warehouse.Warehousing.Inventory.Domain;

namespace Warehouse.Warehousing.Inventory.Application.Abstractions;

/// <summary>Persistence port for the <see cref="Batch"/> aggregate (expiry + QC hold, SKU-wide).</summary>
public interface IBatchRepository : IRepository<Batch, BatchId>
{
    /// <summary>Looks up a batch by its SKU and number — the natural key used at receipt and allocation.</summary>
    Task<Batch?> GetByNumberAsync(Sku sku, BatchNumber number, CancellationToken cancellationToken = default);
}
