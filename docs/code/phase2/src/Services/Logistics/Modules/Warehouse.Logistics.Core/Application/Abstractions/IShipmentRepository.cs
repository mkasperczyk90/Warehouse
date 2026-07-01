using Warehouse.Logistics.Core.Domain;
using Warehouse.SharedKernel.Application;

namespace Warehouse.Logistics.Core.Application.Abstractions;

/// <summary>Persistence port for the <see cref="Shipment"/> aggregate (packing → dispatch).</summary>
public interface IShipmentRepository : IRepository<Shipment, ShipmentId>
{
    /// <summary>The shipment created for an order, or <c>null</c> if it has not been packed yet.</summary>
    Task<Shipment?> GetByOrderAsync(OrderId orderId, CancellationToken cancellationToken = default);
}
