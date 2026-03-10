using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Warehouse.Inventory.Api.Controllers.Inventory.CreateInventory;
using Warehouse.Inventory.Application.Inventory.Commands.CreateInventory;
using Warehouse.Inventory.Application.Inventory.Queries.ListInventories;

namespace Warehouse.Inventory.Api.Controllers.Inventories;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class InventoriesController(IMediator mediator) : ControllerBase
{
	[HttpGet(Name = "GetInventory")]
	[Authorize(Roles = "read")]
	public async Task<IActionResult> Get(CancellationToken cancellationToken)
	{
		var result = await mediator.Send(new ListInventoriesQuery(), cancellationToken);

		// TODO: create extension for Result
		if (result.IsSuccess) return Ok(result.Value);

		return BadRequest(new {
			error = result.Error.Code,
			message = result.Error.Description
		});
	}

	[HttpPost(Name = "PostInventory")]
	[Authorize(Roles = "write")]
	public async Task<IActionResult> Post(CreateInventoryRequest command, CancellationToken cancellationToken)
	{
		var result = await mediator.Send(new  CreateInventoryCommand(
			command.ProductId,
			command.Quantity), cancellationToken);

		// TODO: create extension for Result
		if (result.IsSuccess) return Created($"/api/v1/inventories/{command.ProductId}", result.Value);

		return BadRequest(new {
			error = result.Error.Code,
			message = result.Error.Description
		});
	}
}
