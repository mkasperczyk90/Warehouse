using Warehouse.Logistics.Core.Domain;
using Warehouse.SharedKernel.Application;

namespace Warehouse.Logistics.Core.Application.Abstractions;

/// <summary>Persistence port for the <see cref="InboundDelivery"/> aggregate (ASN → put-away).</summary>
public interface IInboundDeliveryRepository : IRepository<InboundDelivery, DeliveryId>
{
    /// <summary>
    /// Dock slots already booked at a dock within a window — the conflict probe for
    /// <c>DockSchedulingService</c> (a slot spans multiple deliveries, so no aggregate sees them all).
    /// </summary>
    Task<IReadOnlyCollection<DockSlot>> ListBookedSlotsAsync(
        string dockCode, DateTimeOffset windowStart, DateTimeOffset windowEnd, CancellationToken cancellationToken = default);
}
