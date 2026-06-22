using Microsoft.EntityFrameworkCore;
using Warehouse.Logistics.Core.Application.Abstractions;
using Warehouse.Logistics.Core.Domain;

namespace Warehouse.Logistics.Core.Infrastructure.Persistence;

/// <summary>EF Core implementation of the outbound-order persistence port. Order lines are owned,
/// so they load with the aggregate.</summary>
internal sealed class OutboundOrderRepository(LogisticsDbContext context) : IOutboundOrderRepository
{
    public Task<OutboundOrder?> GetByIdAsync(OrderId id, CancellationToken cancellationToken = default) =>
        context.Orders.FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public void Add(OutboundOrder aggregate) => context.Orders.Add(aggregate);

    public void Update(OutboundOrder aggregate) => context.Orders.Update(aggregate);

    public async Task<IReadOnlyCollection<OutboundOrder>> ListByStatusAsync(
        OrderStatus status, CancellationToken cancellationToken = default) =>
        await context.Orders
            .Where(o => o.Status == status)
            .ToListAsync(cancellationToken);
}
