using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Warehouse.Product.Api.Controllers.Products.CreateProduct;
using Warehouse.Product.Application.Products.Commands.CreateProduct;
using Warehouse.Product.Application.Products.Queries.GetProduct;
using Warehouse.Product.Application.Products.Queries.ListProducts;

namespace Warehouse.Product.Api.Controllers.Products;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class ProductsController(IMediator mediator) : ControllerBase
{
	[HttpGet("{id:guid}", Name = "GetProduct")]
	[Authorize(Roles = "read")]
	public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
	{
		var result = await mediator.Send(new GetProductQuery(id), cancellationToken);

		// TODO: create extension for Result
		if (result.IsSuccess) return Ok(result.Value);

		return BadRequest(new {
			error = result.Error.Code,
			message = result.Error.Description
		});
	}

	[HttpGet(Name = "GetProducts")]
	[Authorize(Roles = "read")]
	public async Task<IActionResult> Get(CancellationToken cancellationToken)
	{
		var result = await mediator.Send(new ListProductsQuery(), cancellationToken);

		// TODO: create extension for Result
		if (result.IsSuccess) return Ok(result.Value);

		return BadRequest(new {
			error = result.Error.Code,
			message = result.Error.Description
		});
	}

	[HttpPost(Name = "PostProduct")]
	[Authorize(Roles = "write")]
	public async Task<IActionResult> Post(CreateProductRequest command, CancellationToken cancellationToken)
	{
		var result = await mediator.Send(new  CreateProductCommand(
			command.Name,
			command.Description,
			command.Price), cancellationToken);

		// TODO: create extension for Result
		if (result.IsSuccess) return Ok(result.Value);

		return BadRequest(new {
			error = result.Error.Code,
			message = result.Error.Description
		});
	}
}
