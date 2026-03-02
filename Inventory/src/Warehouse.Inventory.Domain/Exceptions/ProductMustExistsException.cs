using Warehouse.SharedKernel.Exceptions;

namespace Warehouse.Inventory.Domain.Exceptions;

public class ProductMustExistsException()
	: DomainException($"Product does not exists.")
{
	public override string Code => "product_does_not_exists";
}
