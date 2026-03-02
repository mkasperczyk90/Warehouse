using Warehouse.SharedKernel;

namespace Warehouse.Inventory.Domain.Events;

public record InventoryCreatedDomainEvent(Guid ProductId, int Quantity): IDomainEvent;
