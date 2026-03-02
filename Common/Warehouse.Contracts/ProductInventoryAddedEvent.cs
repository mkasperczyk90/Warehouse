namespace Warehouse.Contracts;

public record ProductInventoryAddedEvent(Guid EventId, Guid ProductId, int Quantity, DateTime OccurredAt);

