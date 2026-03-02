namespace Warehouse.Product.Application.Products.Queries.GetProduct;

public sealed record ProductResponse(
	Guid Id,
	string Name,
	decimal Price,
	string Description,
	int Amount,
	DateTime CreatedAt,
	DateTime? UpdatedAt
	);
