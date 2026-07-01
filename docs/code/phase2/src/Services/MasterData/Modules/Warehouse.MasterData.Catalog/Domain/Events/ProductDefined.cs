using Warehouse.SharedKernel.Domain;
using Warehouse.SharedKernel.ValueObjects;

namespace Warehouse.MasterData.Catalog.Domain.Events;

public sealed record ProductDefined(Sku Sku, DateTimeOffset OccurredAt) : IDomainEvent;
