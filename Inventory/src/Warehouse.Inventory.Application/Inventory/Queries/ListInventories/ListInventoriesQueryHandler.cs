using MediatR;
using Warehouse.Inventory.Domain.Interfaces;
using Warehouse.SharedKernel;

namespace Warehouse.Inventory.Application.Inventory.Queries.ListInventories;

public sealed class ListInventoriesQueryHandler(IInventoryRepository inventoryRepository) : IRequestHandler<ListInventoriesQuery, Result<ListInventoriesResponse>>
{
	public async Task<Result<ListInventoriesResponse>> Handle(ListInventoriesQuery request, CancellationToken cancellationToken)
	{
		var inventories = (await inventoryRepository
				.ListAsync(cancellationToken))
			.Select(inventory => new InventoryResponse(
				inventory.Id,
				inventory.ProductId,
				inventory.Quantity,
				inventory.AddedAt,
				inventory.AddedBy));

		var result = new ListInventoriesResponse(inventories.ToList());
		return Result.Success(result);
	}
}
