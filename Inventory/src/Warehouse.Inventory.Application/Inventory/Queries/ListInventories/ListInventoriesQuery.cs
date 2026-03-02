using MediatR;
using Warehouse.Inventory.Application.Inventory.Queries.ListInventories;
using Warehouse.SharedKernel;

namespace Warehouse.Inventory.Application.Inventory.Queries.ListInventories;

public sealed record ListInventoriesQuery : IRequest<Result<ListInventoriesResponse>>;
