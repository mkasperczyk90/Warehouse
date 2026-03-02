namespace Warehouse.Product.Domain.Interfaces;

public interface IProcessedEventRepository
{
	Task InsertAsync(ProcessedEvent.Entities.ProcessedEvent processedEvent, CancellationToken ct = default);
	Task<bool> Exists(Guid eventId, CancellationToken ct = default);
}
