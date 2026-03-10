using Warehouse.Inventory.Application.Http;

namespace Warehouse.Inventory.Infrastructure.Http;

public class ProductServiceClient(HttpClient httpClient) : IProductServiceClient
{
	public async Task<bool> ExistsAsync(Guid productId, CancellationToken ct = default)
	{
		var response = await httpClient.GetAsync($"api/v1.0/products/{productId}", ct);

		return response.IsSuccessStatusCode;
	}
}
