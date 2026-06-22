using Warehouse.Logistics.Core.Application.Abstractions;
using Warehouse.Logistics.Core.Domain;
using Warehouse.SharedKernel.Application;

namespace Warehouse.Logistics.Core.Application.RegisterArrival;

/// <summary>UC-02 — the truck checked in at the dock.</summary>
public sealed record RegisterArrivalCommand(Guid DeliveryId);

public sealed class RegisterArrivalHandler(IInboundDeliveryRepository deliveries, IUnitOfWork unitOfWork)
{
    public async Task HandleAsync(RegisterArrivalCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        var delivery = await deliveries.GetByIdAsync(new DeliveryId(command.DeliveryId), cancellationToken)
            ?? throw new KeyNotFoundException($"Delivery {command.DeliveryId} not found.");

        delivery.RegisterArrival();
        deliveries.Update(delivery);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
