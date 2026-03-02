using MediatR;
using Warehouse.Product.Application.Products.Queries.GetProduct;
using Warehouse.Product.Domain.Interfaces;
using Warehouse.SharedKernel;

namespace Warehouse.Product.Application.Products.Queries.ListProducts;

public sealed class ListProductsQueryHandler(IProductRepository productRepository) : IRequestHandler<ListProductsQuery, Result<ListProductsResponse>>
{
	public async Task<Result<ListProductsResponse>> Handle(ListProductsQuery request, CancellationToken cancellationToken)
	{
		var products = (await productRepository
				.ListAsync(cancellationToken))
			.Select(p => new ProductResponse(p.Id, p.Name, p.Price, p.Description,p.Amount, p.CreatedAt, p.UpdatedAt));

		var result = new ListProductsResponse(products.ToList());
		return Result.Success(result);
	}
}
