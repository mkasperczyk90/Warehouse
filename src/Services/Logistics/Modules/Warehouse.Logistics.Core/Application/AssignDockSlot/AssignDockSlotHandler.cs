using Warehouse.Logistics.Core.Application.Abstractions;
using Warehouse.Logistics.Core.Domain;
using Warehouse.Logistics.Core.Domain.Services;
using Warehouse.SharedKernel.Application;

namespace Warehouse.Logistics.Core.Application.AssignDockSlot;

/// <summary>
/// Books the slot through <see cref="DockSchedulingService"/>, which enforces the cross-aggregate
/// no-overlap rule (the booked slots at that dock come from the repository).
/// </summary>
public sealed class AssignDockSlotHandler(IInboundDeliveryRepository deliveries, IUnitOfWork unitOfWork)
{
    public async Task HandleAsync(AssignDockSlotCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var delivery = await deliveries.GetByIdAsync(new DeliveryId(command.DeliveryId), cancellationToken)
            ?? throw new KeyNotFoundException($"Delivery {command.DeliveryId} not found.");

        var slot = DockSlot.Of(command.DockCode, command.From, command.To);
        var booked = await deliveries.ListBookedSlotsAsync(slot.DockCode, slot.From, slot.To, cancellationToken);

        DockSchedulingService.Schedule(delivery, slot, booked);

        deliveries.Update(delivery);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
