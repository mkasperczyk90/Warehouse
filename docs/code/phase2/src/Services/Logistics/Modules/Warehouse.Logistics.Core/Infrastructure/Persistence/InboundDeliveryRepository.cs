using Microsoft.EntityFrameworkCore;
using Warehouse.Logistics.Core.Application.Abstractions;
using Warehouse.Logistics.Core.Domain;

namespace Warehouse.Logistics.Core.Infrastructure.Persistence;

/// <summary>EF Core implementation of the inbound-delivery persistence port. Lines and the dock
/// slot are owned, so they load with the aggregate.</summary>
internal sealed class InboundDeliveryRepository(LogisticsDbContext context) : IInboundDeliveryRepository
{
    public Task<InboundDelivery?> GetByIdAsync(DeliveryId id, CancellationToken cancellationToken = default) =>
        context.Deliveries.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public void Add(InboundDelivery aggregate) => context.Deliveries.Add(aggregate);

    public void Update(InboundDelivery aggregate) => context.Deliveries.Update(aggregate);

    public async Task<IReadOnlyCollection<DockSlot>> ListBookedSlotsAsync(
        string dockCode, DateTimeOffset windowStart, DateTimeOffset windowEnd, CancellationToken cancellationToken = default)
    {
        var normalized = dockCode.Trim().ToUpperInvariant();

        // Slots overlap the window when they start before it ends and end after it starts.
        // Project the components (not the owned instance) and rebuild via the factory.
        var rows = await context.Deliveries
            .Where(d => d.Slot != null
                && d.Slot.DockCode == normalized
                && d.Slot.From < windowEnd
                && d.Slot.To > windowStart)
            .Select(d => new { d.Slot!.DockCode, d.Slot.From, d.Slot.To })
            .ToListAsync(cancellationToken);

        return rows.Select(r => DockSlot.Of(r.DockCode, r.From, r.To)).ToList();
    }
}
