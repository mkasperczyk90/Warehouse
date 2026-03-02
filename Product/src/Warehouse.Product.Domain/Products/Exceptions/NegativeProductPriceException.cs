using Warehouse.SharedKernel.Exceptions;

namespace Warehouse.Product.Domain.Products.Exceptions;

public class NegativeProductPriceException(decimal price)
	: DomainException($"Product price cannot be negative. Provided price: {price}")
{
	public override string Code => "negative_price";
}
