namespace Warehouse.Inventory.Domain.Interfaces;

public interface IInventoryRepository
{
	Task InsertAsync(Entities.Inventory inventory, CancellationToken ct = default);

	Task UpdateAsync(Entities.Inventory inventory, CancellationToken ct = default);

	Task<IReadOnlyList<Entities.Inventory>> ListAsync(CancellationToken ct = default);
}
