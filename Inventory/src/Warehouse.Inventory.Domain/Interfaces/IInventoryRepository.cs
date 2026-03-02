namespace Warehouse.Inventory.Domain.Interfaces;

public interface IInventoryRepository
{
	Task InsertAsync(Inventory inventory, CancellationToken ct = default);

	Task UpdateAsync(Inventory inventory, CancellationToken ct = default);

	Task<IReadOnlyList<Inventory>> ListAsync(CancellationToken ct = default);
}
