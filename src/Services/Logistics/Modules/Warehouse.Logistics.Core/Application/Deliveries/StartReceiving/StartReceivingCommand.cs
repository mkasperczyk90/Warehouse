using Warehouse.Logistics.Core.Application.Abstractions;
using Warehouse.Logistics.Core.Domain;
using Warehouse.SharedKernel.Application;

namespace Warehouse.Logistics.Core.Application.Deliveries.StartReceiving;

/// <summary>UC-02 — scanning starts; the delivery moves into the receiving state.</summary>
public sealed record StartReceivingCommand(Guid DeliveryId);

public sealed class StartReceivingHandler(IInboundDeliveryRepository deliveries, IUnitOfWork unitOfWork)
{
    public async Task HandleAsync(StartReceivingCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        var delivery = await deliveries.GetByIdAsync(new DeliveryId(command.DeliveryId), cancellationToken)
            ?? throw new KeyNotFoundException($"Delivery {command.DeliveryId} not found.");

        delivery.StartReceiving();
        deliveries.Update(delivery);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
