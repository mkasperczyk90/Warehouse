using Microsoft.EntityFrameworkCore;
using Warehouse.Inventory.Domain.Interfaces;

namespace Warehouse.Inventory.Infrastructure.Persistence.InventoryRepository;

public class InventoryRepository(InventoryDbContext context) : IInventoryRepository
{
	private readonly InventoryDbContext _context = context ?? throw new ArgumentNullException(nameof(context));

	public async Task InsertAsync(Domain.Entities.Inventory inventory, CancellationToken ct = default) => await _context.AddAsync(inventory, ct);

	public Task UpdateAsync(Domain.Entities.Inventory inventory, CancellationToken ct = default)
	{
		_context.Inventory.Update(inventory);
		return Task.CompletedTask;
	}
	public async Task<IReadOnlyList<Domain.Entities.Inventory>> ListAsync(CancellationToken ct = default) =>
		await _context.Inventory
			.AsNoTracking()
			.OrderBy(p => p.ProductId)
			.ToListAsync(ct);
}

