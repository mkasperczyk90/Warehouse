using Warehouse.Logistics.Core.Domain;
using Warehouse.SharedKernel.Application;

namespace Warehouse.Logistics.Core.Application.Abstractions;

/// <summary>Persistence port for the <see cref="OutboundOrder"/> aggregate (order → dispatch).</summary>
public interface IOutboundOrderRepository : IRepository<OutboundOrder, OrderId>
{
    /// <summary>Orders currently in a given lifecycle state, e.g. all <c>Reserved</c> orders awaiting a wave.</summary>
    Task<IReadOnlyCollection<OutboundOrder>> ListByStatusAsync(OrderStatus status, CancellationToken cancellationToken = default);
}
