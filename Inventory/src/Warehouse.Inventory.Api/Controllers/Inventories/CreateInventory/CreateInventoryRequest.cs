namespace Warehouse.Inventory.Api.Controllers.Inventory.CreateInventory;

public class CreateInventoryRequest
{
	public required Guid ProductId { get; set; }
	public required int Quantity { get; set; }
}
