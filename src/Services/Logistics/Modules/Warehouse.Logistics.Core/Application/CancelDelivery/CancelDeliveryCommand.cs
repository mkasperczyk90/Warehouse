using Warehouse.Logistics.Core.Application.Abstractions;
using Warehouse.Logistics.Core.Domain;
using Warehouse.SharedKernel.Application;

namespace Warehouse.Logistics.Core.Application.CancelDelivery;

/// <summary>Cancel an announced delivery (only allowed before arrival).</summary>
public sealed record CancelDeliveryCommand(Guid DeliveryId);

public sealed class CancelDeliveryHandler(IInboundDeliveryRepository deliveries, IUnitOfWork unitOfWork)
{
    public async Task HandleAsync(CancelDeliveryCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        var delivery = await deliveries.GetByIdAsync(new DeliveryId(command.DeliveryId), cancellationToken)
            ?? throw new KeyNotFoundException($"Delivery {command.DeliveryId} not found.");

        delivery.Cancel();
        deliveries.Update(delivery);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
