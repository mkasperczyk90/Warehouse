using MediatR;
using Warehouse.Product.Application.Products.Queries.GetProduct;
using Warehouse.SharedKernel;

namespace Warehouse.Product.Application.Products.Queries.StreamProducts;

public sealed record StreamProductsQuery : IRequest<Result<IAsyncEnumerable<StreamResponse>>>;
