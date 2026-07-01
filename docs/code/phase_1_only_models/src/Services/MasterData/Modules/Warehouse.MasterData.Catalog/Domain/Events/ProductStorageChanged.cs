using Warehouse.SharedKernel.Domain;
using Warehouse.SharedKernel.ValueObjects;

namespace Warehouse.MasterData.Catalog.Domain.Events;

/// <summary>
/// Storage requirements changed. Existing stock is NOT moved automatically —
/// downstream contexts report incompatibilities for a manager to resolve (UC-13).
/// </summary>
public sealed record ProductStorageChanged(
    Sku Sku,
    StorageRequirement Storage,
    DateTimeOffset OccurredAt) : IDomainEvent;
