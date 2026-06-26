using Warehouse.Contracts.Logistics;
using Warehouse.Logistics.Core.Application.Abstractions;
using Warehouse.Logistics.Core.Domain;
using Warehouse.Logistics.Core.Infrastructure.Persistence;
using Warehouse.SharedKernel.ValueObjects;
using Wolverine.EntityFrameworkCore;

namespace Warehouse.Logistics.Core.Application.Orders.ConfirmDispatch;

/// <summary>UC-12 — the carrier collected the packed order: fast-forward its shipment (opened at packing,
/// UC-11) through carrier assignment + pickup notice to <c>Dispatched</c>, record the optional tracking
/// number, and announce <see cref="ShipmentDispatchedV1"/> so Inventory deducts the stock. This is the
/// terminal's one-shot path; the admin dispatch board walks the same states column by column.</summary>
public sealed record ConfirmDispatchCommand(
    Guid OrderId, string CarrierRoleId, string? TrackingNumber, decimal PackageWeightKg);

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
            ?? throw new KeyNotFoundException($"Order {command.OrderId} has no shipment to dispatch.");

        // Fast-forward to ReadyForPickup from wherever the board left it (terminal's direct dispatch).
        if (shipment.Status == ShipmentStatus.AwaitingCarrier)
        {
            shipment.AssignCarrier(new PartyRoleRef(command.CarrierRoleId), pickup: "immediate");
        }

        if (shipment.Status == ShipmentStatus.CarrierAssigned)
        {
            shipment.SendPickupNotice();
        }

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
            shipment.Carrier!.Value.Value,
            command.TrackingNumber,
            DateTimeOffset.UtcNow));

        await outbox.SaveChangesAndFlushMessagesAsync(cancellationToken);
    }
}
