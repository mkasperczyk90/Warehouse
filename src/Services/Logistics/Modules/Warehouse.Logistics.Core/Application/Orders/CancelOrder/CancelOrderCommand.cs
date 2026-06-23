using Warehouse.Contracts.Logistics;
using Warehouse.Logistics.Core.Application.Abstractions;
using Warehouse.Logistics.Core.Domain;
using Warehouse.Logistics.Core.Infrastructure.Persistence;
using Wolverine.EntityFrameworkCore;

namespace Warehouse.Logistics.Core.Application.Orders.CancelOrder;

/// <summary>Cancel an outbound order; Inventory releases its reservations in reaction.</summary>
public sealed record CancelOrderCommand(Guid OrderId);

public sealed class CancelOrderHandler(
    IOutboundOrderRepository orders,
    IDbContextOutbox<LogisticsDbContext> outbox)
{
    public async Task HandleAsync(CancelOrderCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        var order = await orders.GetByIdAsync(new OrderId(command.OrderId), cancellationToken)
            ?? throw new KeyNotFoundException($"Order {command.OrderId} not found.");

        order.Cancel();
        orders.Update(order);

        await outbox.PublishAsync(new OutboundOrderCancelledV1(order.Id.Value, DateTimeOffset.UtcNow));
        await outbox.SaveChangesAndFlushMessagesAsync(cancellationToken);
    }
}
