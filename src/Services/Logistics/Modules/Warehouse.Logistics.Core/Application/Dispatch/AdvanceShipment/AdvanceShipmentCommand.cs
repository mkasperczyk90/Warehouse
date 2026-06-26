using Warehouse.Contracts.Logistics;
using Warehouse.Logistics.Core.Application.Abstractions;
using Warehouse.Logistics.Core.Domain;
using Warehouse.Logistics.Core.Infrastructure.Persistence;
using Warehouse.SharedKernel.Domain;
using Wolverine.EntityFrameworkCore;

namespace Warehouse.Logistics.Core.Application.Dispatch.AdvanceShipment;

/// <summary>UC-12 — advance a shipment one column on the dispatch board: CarrierAssigned → ReadyForPickup
/// (send the pickup notice), or ReadyForPickup → Dispatched (the carrier collected — tracking is stamped
/// and <see cref="ShipmentDispatchedV1"/> announced so Inventory deducts the stock).</summary>
public sealed record AdvanceShipmentCommand(Guid ShipmentId);

public sealed class AdvanceShipmentHandler(
    IShipmentRepository shipments,
    IOutboundOrderRepository orders,
    IDbContextOutbox<LogisticsDbContext> outbox)
{
    public async Task HandleAsync(AdvanceShipmentCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        var shipment = await shipments.GetByIdAsync(new ShipmentId(command.ShipmentId), cancellationToken)
            ?? throw new KeyNotFoundException($"Shipment {command.ShipmentId} not found.");

        switch (shipment.Status)
        {
            case ShipmentStatus.CarrierAssigned:
                shipment.SendPickupNotice();
                shipments.Update(shipment);
                break;

            case ShipmentStatus.ReadyForPickup:
                await DispatchAsync(shipment, cancellationToken);
                return;

            default:
                throw new DomainException(
                    "shipment_cannot_advance",
                    $"Shipment {shipment.Id} is {shipment.Status} and cannot advance from the board.");
        }

        await outbox.SaveChangesAndFlushMessagesAsync(cancellationToken);
    }

    private async Task DispatchAsync(Shipment shipment, CancellationToken cancellationToken)
    {
        shipment.AssignTracking(TrackingNumber.Of(
            $"TRK-{shipment.Id.Value.ToString("N")[..8].ToUpperInvariant()}"));
        shipment.Dispatch();
        shipments.Update(shipment);

        var order = await orders.GetByIdAsync(shipment.OrderId, cancellationToken)
            ?? throw new KeyNotFoundException($"Order {shipment.OrderId.Value} not found for shipment {shipment.Id}.");
        order.MarkDispatched();
        orders.Update(order);

        await outbox.PublishAsync(new ShipmentDispatchedV1(
            order.Id.Value,
            shipment.Id.Value,
            order.Warehouse.Code,
            shipment.Carrier!.Value.Value,
            shipment.Tracking?.Value,
            DateTimeOffset.UtcNow));

        await outbox.SaveChangesAndFlushMessagesAsync(cancellationToken);
    }
}
