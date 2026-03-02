namespace Warehouse.SharedKernel;

// TODO: Should be Fat UoW? Or Separated? Or Just add interface to Repository??
public interface IUnitOfWork
{
	Task<int> CommitAsync(CancellationToken cancellationToken = default);
}
