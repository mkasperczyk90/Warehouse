using Warehouse.SharedKernel;

namespace Warehouse.Product.Domain.Products.Events;

public sealed record ProductCreatedDomainEvent(Guid productId) : IDomainEvent;

