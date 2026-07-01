using Microsoft.EntityFrameworkCore;
using Warehouse.Logistics.Core.Application.Abstractions;
using Warehouse.Logistics.Core.Domain;

namespace Warehouse.Logistics.Core.Infrastructure.Persistence;

/// <summary>EF Core implementation of the shipment persistence port. Packages are owned, so they
/// load with the aggregate.</summary>
internal sealed class ShipmentRepository(LogisticsDbContext context) : IShipmentRepository
{
    public Task<Shipment?> GetByIdAsync(ShipmentId id, CancellationToken cancellationToken = default) =>
        context.Shipments.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public void Add(Shipment aggregate) => context.Shipments.Add(aggregate);

    public void Update(Shipment aggregate) => context.Shipments.Update(aggregate);

    public Task<Shipment?> GetByOrderAsync(OrderId orderId, CancellationToken cancellationToken = default) =>
        context.Shipments.FirstOrDefaultAsync(s => s.OrderId == orderId, cancellationToken);
}
