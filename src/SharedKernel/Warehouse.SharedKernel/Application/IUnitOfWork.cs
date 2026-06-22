namespace Warehouse.SharedKernel.Application;

/// <summary>
/// Commits every aggregate changed during one operation as a single transaction. Dequeuing
/// domain events and writing the transactional outbox happen here, so "publish after save" can
/// never be a separate, droppable step.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>Persists all pending changes; returns the number of state entries written.</summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
