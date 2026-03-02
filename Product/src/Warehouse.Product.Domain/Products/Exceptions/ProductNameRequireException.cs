using Warehouse.SharedKernel.Exceptions;
using ApplicationException = System.ApplicationException;

namespace Warehouse.Product.Domain.Products.Exceptions;

public class ProductNameRequireException()
	: DomainException($"Product name is required.")
{
	public override string Code => "negative_price";
}
