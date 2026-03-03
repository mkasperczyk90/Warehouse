using Warehouse.Inventory.Domain.Entities;

namespace Warehouse.Inventory.Application.Inventory.Queries.ListInventories;

public record InventoryResponse(InventoryId Id, ProductId ProductId, int Quantity, DateTime AddedAt, string AddedBy);

public record ListInventoriesResponse(List<InventoryResponse> Inventories);
