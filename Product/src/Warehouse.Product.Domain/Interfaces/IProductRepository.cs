using Warehouse.Product.Domain.Products.Entities;
using DomainProduct = Warehouse.Product.Domain.Products.Entities.Product;

namespace Warehouse.Product.Domain.Interfaces;

public interface IProductRepository
{
	Task InsertAsync(DomainProduct product, CancellationToken ct = default);

	Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default);

	Task UpdateAsync(DomainProduct product, CancellationToken ct = default);

	Task<IReadOnlyList<DomainProduct>> ListAsync(CancellationToken ct = default);

	Task<DomainProduct?> Get(ProductId productId, CancellationToken ct = default);
}
