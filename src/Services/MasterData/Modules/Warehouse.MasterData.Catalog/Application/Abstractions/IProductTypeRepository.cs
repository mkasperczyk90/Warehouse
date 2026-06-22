using Warehouse.MasterData.Catalog.Domain;
using Warehouse.SharedKernel.Application;

namespace Warehouse.MasterData.Catalog.Application.Abstractions;

/// <summary>Persistence port for the product catalog (the <see cref="ProductType"/> aggregate).</summary>
public interface IProductTypeRepository : IRepository<ProductType, Sku>
{
    /// <summary>Uniqueness probe for <c>CatalogRegistrationPolicy</c>: is this SKU already taken?</summary>
    Task<bool> ExistsAsync(Sku sku, CancellationToken cancellationToken = default);

    /// <summary>Uniqueness probe for <c>CatalogRegistrationPolicy</c>: is this EAN already in use?</summary>
    Task<bool> ExistsByEanAsync(Ean ean, CancellationToken cancellationToken = default);
}
