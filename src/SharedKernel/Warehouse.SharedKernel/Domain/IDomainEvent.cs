namespace Warehouse.SharedKernel.Domain;

/// <summary>
/// A fact that happened inside an aggregate. Domain events stay within the owning
/// bounded context; cross-service communication uses integration events from Warehouse.Contracts.
/// </summary>
public interface IDomainEvent
{
    DateTimeOffset OccurredAt { get; }
}
