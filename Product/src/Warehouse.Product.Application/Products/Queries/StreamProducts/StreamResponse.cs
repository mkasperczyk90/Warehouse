namespace Warehouse.Product.Application.Products.Queries.GetProduct;

public sealed record StreamResponse(
	Guid Id,
	string Name,
	decimal Price,
	string Description,
	int Amount,
	DateTimeOffset CreatedAt,
	DateTimeOffset? UpdatedAt
	);
