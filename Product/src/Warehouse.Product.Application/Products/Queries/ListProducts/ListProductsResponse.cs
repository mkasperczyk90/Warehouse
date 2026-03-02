using Warehouse.Product.Application.Products.Queries.GetProduct;

namespace Warehouse.Product.Application.Products.Queries.ListProducts;

public class ListProductsResponse(List<ProductResponse> products)
{
	public List<ProductResponse> Products { get; init; } = products;
}
