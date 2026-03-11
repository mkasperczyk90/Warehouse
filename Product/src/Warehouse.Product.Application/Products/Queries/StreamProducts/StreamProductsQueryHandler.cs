using MediatR;
using Warehouse.Product.Application.Products.Queries.GetProduct;
using Warehouse.Product.Domain.Interfaces;
using Warehouse.SharedKernel;

namespace Warehouse.Product.Application.Products.Queries.StreamProducts;

public sealed class StreamProductsQueryHandler(IProductRepository productRepository) : IRequestHandler<StreamProductsQuery, Result<IAsyncEnumerable<StreamResponse>>>
{
	public async Task<Result<IAsyncEnumerable<StreamResponse>>> Handle(StreamProductsQuery request, CancellationToken cancellationToken)
	{
		var products = (await productRepository
				.ListAsync(cancellationToken))
			.Select(p => new StreamResponse(p.Id.Value, p.Name, p.Price, p.Description,p.Amount, p.CreatedAt, p.UpdatedAt))
			.ToAsyncEnumerable();

		return Result.Success(products);
	}
}
