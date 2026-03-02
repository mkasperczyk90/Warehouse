using Warehouse.SharedKernel.Exceptions;

namespace Warehouse.Product.Domain.Products.Exceptions;

public class NegativeInventoryQuantityException(decimal quantity)
	: DomainException($"Inventory quantity cannot be negative. Provided quantity: {quantity}")
{
	public override string Code => "negative_quantity";
}
