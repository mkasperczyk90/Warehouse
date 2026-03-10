using Warehouse.SharedKernel;

namespace Warehouse.Inventory.Application.Inventory;

public static class InventoryErrors
{
	public static Error ProductNotFound(Guid productId) => new(
		"Inventory.ProductNotFound",
		$"Product does not exist. ID: ({productId})",
		ErrorType.NotFound);
}
