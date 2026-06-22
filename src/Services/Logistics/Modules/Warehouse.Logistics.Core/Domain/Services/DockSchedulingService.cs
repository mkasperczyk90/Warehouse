using Warehouse.SharedKernel.Domain;

namespace Warehouse.Logistics.Core.Domain.Services;

/// <summary>
/// Assigning a dock slot is a rule that spans many <see cref="InboundDelivery"/> aggregates: two
/// trucks must not occupy the same dock at overlapping times. No single delivery can see the
/// others, so the application layer supplies the slots already booked at that dock (via
/// <c>IInboundDeliveryRepository</c>) and this service enforces the no-overlap rule before
/// delegating to the aggregate's own <see cref="InboundDelivery.AssignDockSlot"/>.
/// </summary>
public static class DockSchedulingService
{
    /// <summary>Books <paramref name="slot"/> for the delivery, rejecting any overlap at the same dock.</summary>
    public static void Schedule(InboundDelivery delivery, DockSlot slot, IEnumerable<DockSlot> bookedSlotsAtDock)
    {
        ArgumentNullException.ThrowIfNull(delivery);
        ArgumentNullException.ThrowIfNull(slot);
        ArgumentNullException.ThrowIfNull(bookedSlotsAtDock);

        foreach (var booked in bookedSlotsAtDock)
        {
            if (booked.DockCode == slot.DockCode && booked.From < slot.To && slot.From < booked.To)
            {
                throw new DomainException(
                    "dock_slot_conflict",
                    $"Dock {slot.DockCode} is already booked {booked.From:u}..{booked.To:u}; " +
                    $"cannot add {slot.From:u}..{slot.To:u}.");
            }
        }

        delivery.AssignDockSlot(slot);
    }
}
