using Warehouse.Contracts.Logistics;
using Warehouse.Logistics.Core.Application.Abstractions;
using Warehouse.Logistics.Core.Domain;
using Warehouse.Logistics.Core.Infrastructure.Persistence;
using Wolverine.EntityFrameworkCore;

namespace Warehouse.Logistics.Core.Application.Deliveries.RegisterArrival;

/// <summary>UC-02 — the truck checked in at the dock.</summary>
public sealed record RegisterArrivalCommand(Guid DeliveryId);

/// <summary>Announces the arrival to Bronze via the transactional outbox (<see cref="DeliveryArrivedV1"/>).</summary>
public sealed class RegisterArrivalHandler(IInboundDeliveryRepository deliveries, IDbContextOutbox<LogisticsDbContext> outbox)
{
    public async Task HandleAsync(RegisterArrivalCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        var delivery = await deliveries.GetByIdAsync(new DeliveryId(command.DeliveryId), cancellationToken)
            ?? throw new KeyNotFoundException($"Delivery {command.DeliveryId} not found.");

        delivery.RegisterArrival();
        deliveries.Update(delivery);
        await outbox.PublishAsync(new DeliveryArrivedV1(delivery.Id.Value, delivery.Warehouse.Code, DateTimeOffset.UtcNow));

        await outbox.SaveChangesAndFlushMessagesAsync(cancellationToken);
    }
}
