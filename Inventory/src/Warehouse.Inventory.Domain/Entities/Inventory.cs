using Warehouse.Inventory.Domain.Exceptions;
using Warehouse.Product.Domain.Products.Exceptions;
using Warehouse.SharedKernel;
using Warehouse.SharedKernel.Constants;

namespace Warehouse.Inventory.Domain.Entities;

public class Inventory: Entity
{
	public InventoryId Id { get; private set; }
	public ProductId ProductId { get; private set; }
	public int Quantity { get; private set; }
	public DateTime AddedAt { get; private set; }
	public string AddedBy { get; private set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
	private Inventory() { }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

	private Inventory(Guid productId, int quantity, string addedBy)
	{
		if (quantity < 0) throw new NegativeInventoryQuantityException(quantity);
		if (productId == Guid.Empty) throw new ProductMustExistsException();

		Id = new(Guid.NewGuid());
		ProductId = new(productId);
		Quantity = quantity;
		AddedAt = DateTime.UtcNow;
		AddedBy = string.IsNullOrWhiteSpace(addedBy) ? UserConstants.SystemName : addedBy;
	}

	public static Inventory Create(Guid productId, int quantity, string addedBy) => new(productId, quantity, addedBy);
}
