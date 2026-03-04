using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Warehouse.Contracts;
using Warehouse.Inventory.Application.Http;
using Warehouse.Inventory.Domain.Events;
using Warehouse.Inventory.Domain.Interfaces;
using Warehouse.SharedKernel;
using Warehouse.SharedKernel.Security;
using Wolverine;

namespace Warehouse.Inventory.Application.Inventory.Commands.CreateInventory;

public class CreateInventoryCommandHandler(
	IUserContext userContext,
	IUnitOfWork unitOfWork,
	IInventoryRepository inventoryRepository,
	IProductServiceClient productClient,
	ILogger<CreateInventoryCommandHandler> logger,
	IMessageBus messageBus,
	TimeProvider timeProvider,
	IValidator<CreateInventoryCommand> validator)
	: IRequestHandler<CreateInventoryCommand, Result<Guid>>
{
	public async Task<Result<Guid>> Handle(CreateInventoryCommand request, CancellationToken cancellationToken)
	{
		var result = await validator.ValidateAsync(request, cancellationToken);
		if (!result.IsValid)
		{
			logger.LogWarning("Inventory creation validation failed for Product {ProductId}. Errors: {Errors}",
				request.ProductId, result.Errors);
			throw new ValidationException(result.Errors);
		}

		var productExists = await productClient.ExistsAsync(request.ProductId, cancellationToken);
		if (!productExists)
		{
			logger.LogWarning("Inventory creation aborted: Product {ProductId} does not exist in Product Service",
				request.ProductId);
			return Result.Failure<Guid>(new Error(
				"Inventory.ProductNotFound",
				$"Product does not exists id: ({request.ProductId})",
				ErrorType.NotFound));
		}

		var inventory = Domain.Entities.Inventory.Create(
			request.ProductId,
			request.Quantity,
			userContext.Username
		);

		inventory.Raise(new InventoryCreatedDomainEvent(request.ProductId,  request.Quantity));

		await inventoryRepository.InsertAsync(inventory, cancellationToken);

		// To keep consistency - outbox pattern needed
		await SendProductInventoryAddedEvent(request, cancellationToken);

		await unitOfWork.CommitAsync(cancellationToken);

		logger.LogInformation("Successfully created inventory {InventoryId} for Product {ProductId} - with quantity {quantity}. Recorded by user {Username}",
			inventory.Id, request.ProductId, userContext.Username, request.Quantity);

		return Result.Success(inventory.Id.Value);
	}

	private async Task SendProductInventoryAddedEvent(CreateInventoryCommand request, CancellationToken cancellationToken)
	{
		var integrationEvent = new ProductInventoryAddedEvent(Guid.NewGuid(), request.ProductId, request.Quantity, timeProvider.GetUtcNow().DateTime);

		logger.LogInformation("Sending ProductInventoryAddedEvent with request {request}", integrationEvent);
		await messageBus.PublishAsync(integrationEvent);
	}
}

