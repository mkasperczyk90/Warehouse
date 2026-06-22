using Microsoft.EntityFrameworkCore;
using Warehouse.Warehousing.Inventory.Application.Abstractions;
using Warehouse.Warehousing.Inventory.Domain;
using Warehouse.Warehousing.Inventory.Domain.Replicas;

namespace Warehouse.Warehousing.Inventory.Infrastructure.Persistence;

/// <summary>EF Core implementation of the product replica port.</summary>
internal sealed class ProductSnapshotRepository(InventoryDbContext context) : IProductSnapshotRepository
{
    public Task<ProductSnapshot?> FindAsync(Sku sku, CancellationToken cancellationToken = default) =>
        context.ProductSnapshots.FirstOrDefaultAsync(p => p.Sku == sku, cancellationToken);

    public void Add(ProductSnapshot snapshot) => context.ProductSnapshots.Add(snapshot);

    public void Update(ProductSnapshot snapshot) => context.ProductSnapshots.Update(snapshot);
}
