using Warehouse.Contracts.Logistics;
using Warehouse.SharedKernel.Application;
using Warehouse.SharedKernel.ValueObjects;
using Warehouse.Warehousing.Inventory.Application.Abstractions;
using Warehouse.Warehousing.Inventory.Domain;
using Warehouse.Warehousing.Inventory.Infrastructure.Persistence;
using Wolverine.EntityFrameworkCore;

namespace Warehouse.Warehousing.Inventory.Application.Consumers;

/// <summary>
/// UC-10 (Inventory side, plan): on <see cref="PickingReleasedV1"/>, turns the order's soft
/// reservations into hard <see cref="Allocation"/>s against concrete stock and replies with the
/// planned picks (<see cref="PicksPlannedV1"/>). Picking order is simplified to location order — the
/// FEFO/wave optimiser is deferred (docs/PLAN.md). No stock moves yet; it moves on pick confirm.
/// </summary>
public sealed class PlanPicksConsumer(
    IStockItemRepository stockItems,
    IStockReservationRepository reservations,
    IDbContextOutbox<InventoryDbContext> outbox)
{
    public async Task Handle(PickingReleasedV1 message, CancellationToken cancellationToken)
    {
        var warehouse = WarehouseCode.Of(message.WarehouseCode);
        var orderRef = OrderRef.Of(message.OrderId.ToString());
        var list = await reservations.ListByOrderAsync(orderRef, cancellationToken);
        var picks = new List<PlannedPickV1>();

        foreach (var reservation in list.Where(r =>
                     r.Status is ReservationStatus.Open or ReservationStatus.PartiallyAllocated))
        {
            var remaining = reservation.Outstanding.Amount;
            var candidates = (await stockItems.ListBySkuAsync(reservation.Sku, warehouse, cancellationToken))
                .Where(i => !i.Available.IsZero)
                .OrderBy(i => i.Location.Value)
                .ToList();

            foreach (var item in candidates)
            {
                if (remaining <= 0)
                {
                    break;
                }

                var take = Math.Min(item.Available.Amount, remaining);
                if (take <= 0)
                {
                    continue;
                }

                var qty = Quantity.Of(take, item.OnHand.Unit);
                item.Allocate(qty, orderRef, reservation.Id);
                reservation.RecordAllocation(qty);
                stockItems.Update(item);
                picks.Add(new PlannedPickV1(
                    item.Location.Value, item.Sku.Value, item.Batch?.Value, take, item.OnHand.Unit.Code));
                remaining -= take;
            }

            reservations.Update(reservation);
        }

        if (picks.Count > 0)
        {
            await outbox.PublishAsync(new PicksPlannedV1(message.OrderId, picks, DateTimeOffset.UtcNow));
        }

        await outbox.SaveChangesAndFlushMessagesAsync(cancellationToken);
    }
}

/// <summary>
/// UC-10 (Inventory side, pick): on <see cref="PickConfirmedV1"/>, consumes the order's hard
/// allocation at that location and deducts the picked quantity from the ledger (one <c>Pick</c>
/// movement). Idempotent — no matching active allocation means it was already picked.
/// </summary>
public sealed class PickStockConsumer(
    IStockItemRepository stockItems,
    IStockLedger ledger,
    IUnitOfWork unitOfWork)
{
    public async Task Handle(PickConfirmedV1 message, CancellationToken cancellationToken)
    {
        var sku = Sku.Of(message.Sku);
        var batch = message.BatchNumber is { } b ? BatchNumber.Of(b) : null;
        var location = LocationCode.Of(message.Location);

        var item = await stockItems.GetAtAsync(sku, batch, location, cancellationToken);
        if (item is null)
        {
            return;
        }

        var orderRef = OrderRef.Of(message.OrderId.ToString());
        var allocation = item.Allocations
            .FirstOrDefault(a => a.Status == AllocationStatus.Active && a.OrderRef == orderRef);
        if (allocation is null)
        {
            return;
        }

        var take = Math.Min(message.Quantity, allocation.Quantity.Amount);
        if (take <= 0)
        {
            return;
        }

        ledger.Append(item.Pick(allocation.Id, Quantity.Of(take, item.OnHand.Unit), performedBy: "pick"));
        stockItems.Update(item);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
