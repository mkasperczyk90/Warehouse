using Microsoft.EntityFrameworkCore;
using Warehouse.Warehousing.Inventory.Application.Abstractions;
using Warehouse.Warehousing.Inventory.Domain;

namespace Warehouse.Warehousing.Inventory.Infrastructure.Persistence;

/// <summary>EF Core implementation of the batch persistence port.</summary>
internal sealed class BatchRepository(InventoryDbContext context) : IBatchRepository
{
    public Task<Batch?> GetByIdAsync(BatchId id, CancellationToken cancellationToken = default) =>
        context.Batches.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    public void Add(Batch aggregate) => context.Batches.Add(aggregate);

    public void Update(Batch aggregate) => context.Batches.Update(aggregate);

    public Task<Batch?> GetByNumberAsync(Sku sku, BatchNumber number, CancellationToken cancellationToken = default) =>
        context.Batches.FirstOrDefaultAsync(b => b.Sku == sku && b.Number == number, cancellationToken);
}
