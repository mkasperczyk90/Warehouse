using Warehouse.Logistics.Core.Domain;
using Warehouse.SharedKernel.Application;

namespace Warehouse.Logistics.Core.Application.Abstractions;

/// <summary>Persistence port for the <see cref="PickList"/> aggregate.</summary>
public interface IPickListRepository : IRepository<PickList, PickListId>
{
    /// <summary>The pick list generated for an order (used when completing picking / packing).</summary>
    Task<PickList?> GetByOrderAsync(OrderId orderId, CancellationToken cancellationToken = default);
}
