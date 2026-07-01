using Microsoft.EntityFrameworkCore;
using Warehouse.MasterData.Catalog.Application.Abstractions;
using Warehouse.MasterData.Catalog.Domain;

namespace Warehouse.MasterData.Catalog.Infrastructure.Persistence;

/// <summary>EF Core implementation of the catalog persistence port. Owned collections
/// (unit conversions) are loaded automatically with the aggregate.</summary>
internal sealed class ProductTypeRepository(CatalogDbContext context) : IProductTypeRepository
{
    public Task<ProductType?> GetByIdAsync(Sku id, CancellationToken cancellationToken = default) =>
        context.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public void Add(ProductType aggregate) => context.Products.Add(aggregate);

    public void Update(ProductType aggregate) => context.Products.Update(aggregate);

    public Task<bool> ExistsAsync(Sku sku, CancellationToken cancellationToken = default) =>
        context.Products.AnyAsync(p => p.Id == sku, cancellationToken);

    public Task<bool> ExistsByEanAsync(Ean ean, CancellationToken cancellationToken = default) =>
        context.Products.AnyAsync(p => p.Ean == ean, cancellationToken);
}
