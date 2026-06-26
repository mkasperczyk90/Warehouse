using Warehouse.Logistics.Core.Application.Abstractions;
using Warehouse.Logistics.Core.Domain;
using Warehouse.SharedKernel.Application;
using Warehouse.SharedKernel.ValueObjects;

namespace Warehouse.Logistics.Core.Application.Orders.MarkPacked;

/// <summary>UC-11 — picking finished and goods are packed (Picking → Packed).</summary>
public sealed record MarkPackedCommand(Guid OrderId);

/// <summary>
/// Marks the order <c>Packed</c> and opens its <see cref="Shipment"/> in <c>AwaitingCarrier</c>, so it
/// lands on the dispatch board (UC-11 → UC-12). Per-package weight/dimensions are captured at packing;
/// without a scale in this flow the shipment opens with one default carton until that capture exists.
/// </summary>
public sealed class MarkPackedHandler(
    IOutboundOrderRepository orders, IShipmentRepository shipments, IUnitOfWork unitOfWork)
{
    private const decimal DefaultPackageWeightKg = 10m;
    private static readonly PackageDimensions DefaultCarton = PackageDimensions.Of(60, 40, 40);

    public async Task HandleAsync(MarkPackedCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        var order = await orders.GetByIdAsync(new OrderId(command.OrderId), cancellationToken)
            ?? throw new KeyNotFoundException($"Order {command.OrderId} not found.");

        order.MarkPacked();
        orders.Update(order);

        var shipment = Shipment.CreateAwaitingCarrier(order.Id);
        shipment.AddPackage(Weight.FromKilograms(DefaultPackageWeightKg), DefaultCarton);
        shipments.Add(shipment);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
