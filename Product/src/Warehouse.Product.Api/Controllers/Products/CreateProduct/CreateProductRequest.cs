namespace Warehouse.Product.Api.Controllers.Products.CreateProduct;

public class CreateProductRequest
{
	public required string Name { get; set; }
	public required string Description { get; set; }
	public required decimal Price { get; set; }
}
