using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Warehouse.Product.Domain.Interfaces;
using Warehouse.Product.Domain.Products.Events;
using Warehouse.SharedKernel;

namespace Warehouse.Product.Application.Products.Commands.CreateProduct;

public class CreateProductCommandHandler(
	ILogger<CreateProductCommandHandler> logger,
	IUnitOfWork unitOfWork,
	IProductRepository productRepository,
	IValidator<CreateProductCommand> validator) : IRequestHandler<CreateProductCommand, Result<Guid>>
{
	public async Task<Result<Guid>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
	{
		var result = await validator.ValidateAsync(request, cancellationToken);
		if (!result.IsValid)
		{
			logger.LogWarning("Validation failed for CreateProductCommand: {Errors}", result.Errors);
			throw new ValidationException(result.Errors);
		}

		if (await productRepository.ExistsByNameAsync(request.Name, cancellationToken))
		{
			logger.LogWarning("CreateProduct failed: Name {ProductName} already exists.", request.Name);
			return Result.Failure<Guid>(new Error(
				"Product.DuplicateName", // TODO: Move to static "ProductErrors"
				"Product already exists.",
				ErrorType.Conflict));
		}

		var product = Domain.Products.Entities.Product.Create(
			request.Name,
			request.Price,
			request.Description
		);

		product.Raise(new ProductCreatedDomainEvent(product.Id));

		await productRepository.InsertAsync(product, cancellationToken);
		await unitOfWork.CommitAsync(cancellationToken);

		logger.LogInformation("Product {ProductId} successfully created with name {ProductName}", product.Id, product.Name);
		return Result.Success(product.Id);
	}
}
