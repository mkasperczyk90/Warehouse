using Microsoft.Extensions.Logging;
using Warehouse.Contracts;
using Warehouse.Product.Domain.Interfaces;
using Warehouse.SharedKernel;

namespace Warehouse.Product.Application.Products.IntegrationEvents;

public class ProductInventoryAddedConsumer(
	ILogger<ProductInventoryAddedConsumer> logger,
	IProcessedEventRepository processedEventRepository,
	IProductRepository productRepository,
	IUnitOfWork unitOfWork
	)
{
	public async ValueTask ConsumeAsync(ProductInventoryAddedEvent message, CancellationToken cancellationToken)
	{
		logger.LogInformation("Product inventory update for {eventId} already processed for product {productId}", message.EventId, message.ProductId);

		var alreadyProcessed = await processedEventRepository.Exists(message.EventId, cancellationToken);

		if (alreadyProcessed)
		{
			logger.LogInformation("Product inventory update for {eventId} already processed", message.EventId);
			return;
		}

		var product = await productRepository.Get(new(message.ProductId), cancellationToken);
		if (product == null)
		{
			logger.LogInformation("Product not found with id {productId}", message.EventId);
			return;
		}
		product.IncreaseStock(message.Quantity);

		await processedEventRepository.InsertAsync(new(message.EventId), cancellationToken);

		await unitOfWork.CommitAsync(cancellationToken);
		logger.LogInformation("Successfully process ProductInventoryAddedConsumer {eventId}, {productId}", message.EventId, message.ProductId);
	}
}
