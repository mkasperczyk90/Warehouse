using Warehouse.Contracts.Logistics;
using Warehouse.Logistics.Core.Application.Abstractions;
using Warehouse.Logistics.Core.Domain;
using Warehouse.Logistics.Core.Infrastructure.Persistence;
using Wolverine.EntityFrameworkCore;

namespace Warehouse.Logistics.Core.Application.Orders.StartPicking;

/// <summary>UC-10 — release a reserved order to the floor (Reserved → Picking) and ask Inventory to
/// plan the concrete picks (FEFO hard allocation) via the transactional outbox.</summary>
public sealed record StartPickingCommand(Guid OrderId);

public sealed class StartPickingHandler(
    IOutboundOrderRepository orders,
    IDbContextOutbox<LogisticsDbContext> outbox)
{
    public async Task HandleAsync(StartPickingCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        var order = await orders.GetByIdAsync(new OrderId(command.OrderId), cancellationToken)
            ?? throw new KeyNotFoundException($"Order {command.OrderId} not found.");

        order.StartPicking();
        orders.Update(order);

        await outbox.PublishAsync(new PickingReleasedV1(order.Id.Value, order.Warehouse.Code, DateTimeOffset.UtcNow));
        await outbox.SaveChangesAndFlushMessagesAsync(cancellationToken);
    }
}
