namespace Warehouse.Inventory.Application.Inventory.Queries.ListInventories;

public record InventoryResponse(Guid Id, Guid ProductId, int Quantity, DateTime AddedAt, string AddedBy);

public record ListInventoriesResponse(List<InventoryResponse> Inventories);
