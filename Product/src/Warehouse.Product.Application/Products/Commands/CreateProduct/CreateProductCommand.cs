using MediatR;
using Warehouse.SharedKernel;

namespace Warehouse.Product.Application.Products.Commands.CreateProduct;

public sealed record CreateProductCommand(
	string Name,
	string Description,
	decimal Price) : IRequest<Result<Guid>>;
