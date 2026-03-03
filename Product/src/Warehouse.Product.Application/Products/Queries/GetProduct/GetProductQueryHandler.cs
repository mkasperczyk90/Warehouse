using MediatR;
using Microsoft.Extensions.Logging;
using Warehouse.Product.Domain.Interfaces;
using Warehouse.SharedKernel;

namespace Warehouse.Product.Application.Products.Queries.GetProduct;

public sealed class GetProductQueryHandler(ILogger<GetProductQueryHandler> logger, IProductRepository productRepository) : IRequestHandler<GetProductQuery, Result<ProductResponse>>
{
	public async Task<Result<ProductResponse>> Handle(GetProductQuery request, CancellationToken cancellationToken)
	{
		logger.LogInformation("Trying to get Product with id {productId}", request.Id);

		var product = (await productRepository
				.ListAsync(cancellationToken))
			.FirstOrDefault(p => p.Id.Value == request.Id);

		if (product is null)
		{
			logger.LogInformation("Product not found with id {productId}", request.Id);
			return Result<ProductResponse>.ValidationFailure(
				new Error( // TODO: Create ProductErrors
					"Product.NotFound",
					"Missing Prodct",
					ErrorType.NotFound
					));
		}

		return Result.Success(
			new ProductResponse(
				product.Id.Value,
				product.Name,
				product.Price,
				product.Description,
				product.Amount,
				product.CreatedAt,
				product.UpdatedAt
			));
	}
}
