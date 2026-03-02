using MediatR;
using Warehouse.SharedKernel;

namespace Warehouse.Product.Application.Products.Queries.ListProducts;

public sealed record ListProductsQuery : IRequest<Result<ListProductsResponse>>;
