using Warehouse.Contracts.Logistics;
using Warehouse.Logistics.Core.Application.Abstractions;
using Warehouse.Logistics.Core.Domain;
using Warehouse.SharedKernel.Application;

namespace Warehouse.Logistics.Core.Application.Consumers;

/// <summary>
/// Closes the inbound choreography: Inventory's replies advance the delivery's process state.
/// Both handlers are idempotent — a redelivered message that no longer matches the expected state
/// is ignored rather than throwing (which would dead-letter a duplicate).
/// </summary>
public sealed class GoodsReceivedConsumer(IInboundDeliveryRepository deliveries, IUnitOfWork unitOfWork)
{
    /// <summary>Inventory brought the receipt on stock in the dock buffer → start put-away.</summary>
    public async Task Handle(GoodsReceivedV1 message, CancellationToken cancellationToken)
    {
        var delivery = await deliveries.GetByIdAsync(new DeliveryId(message.DeliveryId), cancellationToken);
        if (delivery is null || delivery.Status != DeliveryStatus.Received)
        {
            return;
        }

        delivery.StartPutAway();
        deliveries.Update(delivery);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

public sealed class PutAwayCompletedConsumer(IInboundDeliveryRepository deliveries, IUnitOfWork unitOfWork)
{
    /// <summary>Every received line is stored → complete the delivery.</summary>
    public async Task Handle(PutAwayCompletedV1 message, CancellationToken cancellationToken)
    {
        var delivery = await deliveries.GetByIdAsync(new DeliveryId(message.DeliveryId), cancellationToken);
        if (delivery is null || delivery.Status != DeliveryStatus.PutAwayInProgress)
        {
            return;
        }

        delivery.CompletePutAway();
        deliveries.Update(delivery);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
