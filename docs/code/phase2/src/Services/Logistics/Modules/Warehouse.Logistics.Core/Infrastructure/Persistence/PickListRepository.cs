using Microsoft.EntityFrameworkCore;
using Warehouse.Logistics.Core.Application.Abstractions;
using Warehouse.Logistics.Core.Domain;

namespace Warehouse.Logistics.Core.Infrastructure.Persistence;

/// <summary>EF Core implementation of the pick-list persistence port. Tasks are owned, so they
/// load with the aggregate.</summary>
internal sealed class PickListRepository(LogisticsDbContext context) : IPickListRepository
{
    public Task<PickList?> GetByIdAsync(PickListId id, CancellationToken cancellationToken = default) =>
        context.PickLists.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public void Add(PickList aggregate) => context.PickLists.Add(aggregate);

    public void Update(PickList aggregate) => context.PickLists.Update(aggregate);

    public Task<PickList?> GetByOrderAsync(OrderId orderId, CancellationToken cancellationToken = default) =>
        context.PickLists.FirstOrDefaultAsync(p => p.OrderId == orderId, cancellationToken);
}
