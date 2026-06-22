using Microsoft.EntityFrameworkCore;
using Warehouse.Warehousing.Inventory.Application.Abstractions;
using Warehouse.Warehousing.Inventory.Domain;

namespace Warehouse.Warehousing.Inventory.Infrastructure.Persistence;

/// <summary>EF Core implementation of the soft-reservation persistence port.</summary>
internal sealed class StockReservationRepository(InventoryDbContext context) : IStockReservationRepository
{
    public Task<StockReservation?> GetByIdAsync(StockReservationId id, CancellationToken cancellationToken = default) =>
        context.StockReservations.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public void Add(StockReservation aggregate) => context.StockReservations.Add(aggregate);

    public void Update(StockReservation aggregate) => context.StockReservations.Update(aggregate);

    public async Task<IReadOnlyCollection<StockReservation>> ListByOrderAsync(
        OrderRef orderRef, CancellationToken cancellationToken = default) =>
        await context.StockReservations
            .Where(r => r.OrderRef == orderRef)
            .ToListAsync(cancellationToken);
}
