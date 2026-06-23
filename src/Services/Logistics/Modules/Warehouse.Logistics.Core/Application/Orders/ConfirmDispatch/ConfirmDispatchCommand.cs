using Warehouse.Contracts.Logistics;
using Warehouse.Logistics.Core.Application.Abstractions;
using Warehouse.Logistics.Core.Domain;
using Warehouse.Logistics.Core.Infrastructure.Persistence;
using Warehouse.SharedKernel.ValueObjects;
using Wolverine.EntityFrameworkCore;

namespace Warehouse.Logistics.Core.Application.Orders.ConfirmDispatch;

/// <summary>UC-12 — the carrier collected the packed order: open the shipment for the assigned carrier,
/// record the handed-over package (weight) and optional tracking number, dispatch it, and announce
/// <see cref="ShipmentDispatchedV1"/> so Inventory deducts the stock. The order must already be
/// <c>Packed</c> (UC-11).</summary>
public sealed record ConfirmDispatchCommand(
    Guid OrderId, Guid CarrierRoleId, string? TrackingNumber, decimal PackageWeightKg);

public sealed class ConfirmDispatchHandler(
    IOutboundOrderRepository orders,
    IShipmentRepository shipments,
    IDbContextOutbox<LogisticsDbContext> outbox)
{
    // Dimensions aren't captured at the dispatch handover (the operator only weighs the parcel); a
    // single consolidated package defaults to a standard carton until the packing flow (UC-11)
    // records per-package dimensions.
    private static readonly PackageDimensions DefaultCarton = PackageDimensions.Of(60, 40, 40);

    public async Task HandleAsync(ConfirmDispatchCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        var orderId = new OrderId(command.OrderId);
        var order = await orders.GetByIdAsync(orderId, cancellationToken)
            ?? throw new KeyNotFoundException($"Order {command.OrderId} not found.");

        var shipment = Shipment.CreateFor(orderId, new PartyRoleRef(command.CarrierRoleId));
        shipment.AddPackage(Weight.FromKilograms(command.PackageWeightKg), DefaultCarton);
        shipment.MarkReadyForPickup();

        if (command.TrackingNumber is { } tracking)
        {
            shipment.AssignTracking(TrackingNumber.Of(tracking));
        }

        shipment.Dispatch();
        shipments.Add(shipment);

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
