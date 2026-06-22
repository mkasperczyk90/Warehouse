using Warehouse.SharedKernel.Domain;

namespace Warehouse.SharedKernel.Application;

/// <summary>
/// Persistence port for an aggregate root. It lives with the application abstractions, not the
/// domain: the domain enforces invariants and raises events; loading and saving aggregates is an
/// application concern, implemented by Infrastructure (EF Core) later. Repositories deal in whole
/// aggregates only — one row per consistency boundary — never in child entities directly.
/// </summary>
/// <typeparam name="TAggregate">The aggregate root type.</typeparam>
/// <typeparam name="TId">The aggregate's strongly-typed identity.</typeparam>
public interface IRepository<TAggregate, TId>
    where TAggregate : AggregateRoot<TId>
    where TId : notnull
{
    /// <summary>Loads an aggregate by its identity, or <c>null</c> if it does not exist.</summary>
    Task<TAggregate?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);

    /// <summary>Registers a new aggregate to be inserted on the next <see cref="IUnitOfWork.SaveChangesAsync"/>.</summary>
    void Add(TAggregate aggregate);

    /// <summary>Marks a tracked aggregate as modified (no-op for change-tracking implementations).</summary>
    void Update(TAggregate aggregate);
}
