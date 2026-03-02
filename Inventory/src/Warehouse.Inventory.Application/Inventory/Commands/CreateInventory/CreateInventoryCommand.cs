using MediatR;
using Warehouse.SharedKernel;

namespace Warehouse.Inventory.Application.Inventory.Commands.CreateInventory;

public sealed record CreateInventoryCommand(
	Guid ProductId,
	int Quantity) : IRequest<Result<Guid>>;
