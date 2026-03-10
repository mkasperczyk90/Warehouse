using Warehouse.SharedKernel;

namespace Warehouse.Product.Application.Products;

public static class ProductErrors
{
	public static readonly Error DuplicateName = new(
		"Product.DuplicateName",
		"The product already exists.",
		ErrorType.Validation);

	public static Error NotFound(Guid productId) => new(
		"Product.NotFound",
		$"Product not found with id {productId}",
		ErrorType.NotFound);
}
