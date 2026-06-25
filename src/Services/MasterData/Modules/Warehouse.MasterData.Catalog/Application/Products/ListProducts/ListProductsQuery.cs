using Microsoft.EntityFrameworkCore;
using Warehouse.MasterData.Catalog.Domain;
using Warehouse.MasterData.Catalog.Infrastructure.Persistence;

namespace Warehouse.MasterData.Catalog.Application.Products.ListProducts;

/// <summary>List product cards, optionally filtered to one category.</summary>
public sealed record ListProductsQuery(ProductCategory? Category);

public sealed record ProductSummaryDto(
    string Sku,
    string Name,
    string Category,
    string BaseUnit,
    string Storage,
    bool IsBatchTracked);

public sealed class ListProductsHandler(CatalogDbContext db)
{
    public async Task<IReadOnlyList<ProductSummaryDto>> HandleAsync(
        ListProductsQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var products = db.Products.AsNoTracking();
        if (query.Category is { } category)
        {
            products = products.Where(p => p.Category == category);
        }

        // Project to value-converted scalars on the server; format the enum/storage in memory (the enum
        // is stored as text but ToString() over it does not translate cleanly).
        var rows = await products
            .OrderBy(p => p.Id)
            .Select(p => new
            {
                Sku = p.Id,
                p.Name,
                p.Category,
                p.BaseUnit,
                p.Storage.RequiresColdChain,
                p.Storage.IsHazardous,
                p.IsBatchTracked,
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(r => new ProductSummaryDto(
                r.Sku.Value,
                r.Name,
                r.Category.ToString(),
                r.BaseUnit.Code,
                r.RequiresColdChain ? ProductStorageMap.ColdChain
                    : r.IsHazardous ? ProductStorageMap.Hazardous
                    : ProductStorageMap.Ambient,
                r.IsBatchTracked))
            .ToList();
    }
}
