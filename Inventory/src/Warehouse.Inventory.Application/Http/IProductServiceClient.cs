namespace Warehouse.Inventory.Application.Http;

public interface IProductServiceClient
{
	Task<bool> ExistsAsync(Guid productId, CancellationToken ct = default);
}
