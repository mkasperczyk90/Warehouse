using Warehouse.Logistics.Core.Application.Abstractions;
using Warehouse.Logistics.Core.Domain;
using Warehouse.Logistics.Core.Infrastructure.Persistence;
using Warehouse.SharedKernel.Domain;
using Wolverine.EntityFrameworkCore;

namespace Warehouse.Logistics.Core.Application.Orders.ResolvePartialOrder;

/// <summary>
/// UC-09 — the coordinator's decision on a partially-reserved order: <c>split</c> (ship the reserved
/// portion now, backorder the rest → order proceeds as Reserved) or <c>hold</c> (wait for stock to cover
/// the whole order → stays partially reserved). A status/decision change on this aggregate only; the
/// soft reservations already live in Inventory and are untouched here.
/// </summary>
public sealed record ResolvePartialOrderCommand(Guid OrderId, string Decision);

public sealed class ResolvePartialOrderHandler(
    IOutboundOrderRepository orders,
    IDbContextOutbox<LogisticsDbContext> outbox)
{
    public async Task HandleAsync(ResolvePartialOrderCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var order = await orders.GetByIdAsync(new OrderId(command.OrderId), cancellationToken)
            ?? throw new KeyNotFoundException($"Order {command.OrderId} not found.");

        switch (command.Decision?.ToLowerInvariant())
        {
            case "split":
                order.SplitForAvailable();
                break;
            case "hold":
                order.HoldForStock();
                break;
            default:
                throw new DomainException(
                    "order_decision_invalid", $"Unknown order decision '{command.Decision}'. Expected split or hold.");
        }

        orders.Update(order);
        await outbox.SaveChangesAndFlushMessagesAsync(cancellationToken);
    }
}
