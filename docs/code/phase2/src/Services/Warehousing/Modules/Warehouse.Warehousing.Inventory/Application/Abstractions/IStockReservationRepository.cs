using Warehouse.SharedKernel.Application;
using Warehouse.Warehousing.Inventory.Domain;

namespace Warehouse.Warehousing.Inventory.Application.Abstractions;

/// <summary>Persistence port for the soft <see cref="StockReservation"/> aggregate.</summary>
public interface IStockReservationRepository : IRepository<StockReservation, StockReservationId>
{
    /// <summary>All reservations made for one outbound order (released together when the order is cancelled).</summary>
    Task<IReadOnlyCollection<StockReservation>> ListByOrderAsync(OrderRef orderRef, CancellationToken cancellationToken = default);
}
