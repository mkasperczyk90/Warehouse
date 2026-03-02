using Microsoft.EntityFrameworkCore;
using Warehouse.Product.Domain.Interfaces;
using DomainProduct = Warehouse.Product.Domain.Products.Entities.Product;

namespace Warehouse.Product.Infrastructure.Persistence.ProductRepository;

public class ProductRepository(ProductDbContext context) : IProductRepository
{
	private readonly ProductDbContext _context = context ?? throw new ArgumentNullException(nameof(context));

	public async Task InsertAsync(DomainProduct product, CancellationToken ct = default) => await _context.Products.AddAsync(product, ct);

	public async Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default) =>
		await context.Products
			.AnyAsync(p => p.Name == name, ct);

	public Task UpdateAsync(DomainProduct product, CancellationToken ct = default)
	{
		_context.Products.Update(product);
		return Task.CompletedTask;
	}

	public async Task<DomainProduct?> Get(Guid productId, CancellationToken ct = default) =>
		await _context.Products.FirstOrDefaultAsync(p => p.Id == productId, cancellationToken: ct);

	public async Task<IReadOnlyList<DomainProduct>> ListAsync(CancellationToken ct = default) =>
		await _context.Products
			.AsNoTracking()
			.OrderBy(p => p.Name)
			.ToListAsync(ct);
}
