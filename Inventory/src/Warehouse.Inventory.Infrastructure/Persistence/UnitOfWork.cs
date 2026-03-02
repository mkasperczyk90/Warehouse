using MediatR;
using Warehouse.SharedKernel;

namespace Warehouse.Inventory.Infrastructure.Persistence;

public class UnitOfWork(InventoryDbContext inventoryContext, IMediator mediator) : IUnitOfWork
{
	public async Task<int> CommitAsync(CancellationToken cancellationToken = default)
	{
		var result = await inventoryContext.SaveChangesAsync(cancellationToken);

		// TODO: Make it generic for other contexts and move to shared kernel
		var entitiesWithEvents = inventoryContext.ChangeTracker.Entries<Entity>()
			.Select(e => e.Entity)
			.Where(e => e.DomainEvents.Count > 0)
			.ToList();

		foreach (var entity in entitiesWithEvents)
		{
			var events = entity.DomainEvents.ToList();
			entity.ClearDomainEvents();

			foreach (var domainEvent in events)
			{
				await mediator.Publish(domainEvent, cancellationToken);
			}
		}
		return result;
	}
}
