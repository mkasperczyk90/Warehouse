using Warehouse.Contracts.Logistics;
using Warehouse.Logistics.Core.Application.Abstractions;
using Warehouse.Logistics.Core.Domain;
using Warehouse.Logistics.Core.Infrastructure.Persistence;
using Wolverine.EntityFrameworkCore;

namespace Warehouse.Logistics.Core.Application.ConfirmDispatch;

/// <summary>UC-12 — the carrier collected the packed order: assign the tracking number, dispatch the
/// (already-packed) shipment, and announce <see cref="ShipmentDispatchedV1"/> so Inventory deducts the
/// stock. The shipment and its packages are built first by the packing flow (UC-11).</summary>
public sealed record ConfirmDispatchCommand(Guid OrderId, string? TrackingNumber);

public sealed class ConfirmDispatchHandler(
    IOutboundOrderRepository orders,
    IShipmentRepository shipments,
    IDbContextOutbox<LogisticsDbContext> outbox)
{
    public async Task HandleAsync(ConfirmDispatchCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        var orderId = new OrderId(command.OrderId);
        var order = await orders.GetByIdAsync(orderId, cancellationToken)
            ?? throw new KeyNotFoundException($"Order {command.OrderId} not found.");
        var shipment = await shipments.GetByOrderAsync(orderId, cancellationToken)
            ?? throw new DomainException(
                "shipment_not_packed", $"Order {command.OrderId} has no shipment yet — pack it first (UC-11).");

        if (command.TrackingNumber is { } tracking)
        {
            shipment.AssignTracking(TrackingNumber.Of(tracking));
        }

        shipment.Dispatch();
        shipments.Update(shipment);

        order.MarkDispatched();
        orders.Update(order);

        await outbox.PublishAsync(new ShipmentDispatchedV1(
            order.Id.Value,
            shipment.Id.Value,
            order.Warehouse.Code,
            shipment.Carrier.Value,
            command.TrackingNumber,
            DateTimeOffset.UtcNow));

        await outbox.SaveChangesAndFlushMessagesAsync(cancellationToken);
    }
}
