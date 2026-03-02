using MediatR;
using Microsoft.Extensions.Logging;
using Warehouse.Product.Domain.Products.Events;

namespace Warehouse.Product.Application.Products.Events;

public class ProductCreatedDomainEventHandler(ILogger<ProductCreatedDomainEventHandler> logger)
	: INotificationHandler<ProductCreatedDomainEvent>
{
	public Task Handle(ProductCreatedDomainEvent notification, CancellationToken cancellationToken)
	{
		logger.LogInformation("Domain Event Handled: Inventory added with ID {Id}", notification.productId);

		return Task.CompletedTask;
	}
}
