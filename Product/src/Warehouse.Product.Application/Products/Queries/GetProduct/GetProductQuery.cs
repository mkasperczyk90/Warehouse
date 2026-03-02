using MediatR;
using Warehouse.SharedKernel;

namespace Warehouse.Product.Application.Products.Queries.GetProduct;

public sealed record GetProductQuery(Guid Id) : IRequest<Result<ProductResponse>>;
