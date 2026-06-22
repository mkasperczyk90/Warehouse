using Warehouse.Contracts.Logistics;
using Warehouse.SharedKernel.Application;
using Warehouse.SharedKernel.ValueObjects;
using Warehouse.Warehousing.Inventory.Application.Abstractions;
using Warehouse.Warehousing.Inventory.Domain;
using Warehouse.Warehousing.Inventory.Domain.Services;
using Warehouse.Warehousing.Inventory.Infrastructure.Persistence;
using Wolverine.EntityFrameworkCore;

namespace Warehouse.Warehousing.Inventory.Application.Consumers;

/// <summary>
/// UC-09 (Inventory side): reacts to <see cref="OutboundOrderPlacedV1"/> by making a soft
/// <see cref="StockReservation"/> against available-to-promise for each line, then replying with
/// <see cref="StockReservedV1"/> (<c>Fully</c> = every line fully promised). ATP spans all stock of
/// the SKU minus what is already promised, so it is computed in the application layer (the
/// <see cref="ReservationService"/> only gates on the number it is given).
/// </summary>
public sealed class ReserveStockConsumer(
    IStockItemRepository stockItems,
    IStockReservationRepository reservations,
    IDbContextOutbox<InventoryDbContext> outbox)
{
    public async Task Handle(OutboundOrderPlacedV1 message, CancellationToken cancellationToken)
    {
        var warehouse = WarehouseCode.Of(message.WarehouseCode);
        var orderRef = OrderRef.Of(message.OrderId.ToString());
        var fully = true;

        foreach (var line in message.Lines)
        {
            var sku = Sku.Of(line.Sku);
            var unit = UnitOfMeasure.FromCode(line.Unit);
            var atp = await AvailableToPromiseAsync(sku, warehouse, cancellationToken);
            var toReserve = Math.Min(line.Quantity, atp);
            if (toReserve <= 0)
            {
                fully = false;
                continue;
            }

            var reservation = ReservationService.Reserve(
                sku, warehouse, orderRef, Quantity.Of(toReserve, unit), Quantity.Of(atp, unit));
            reservations.Add(reservation);
            if (toReserve < line.Quantity)
            {
                fully = false;
            }
        }

        await outbox.PublishAsync(new StockReservedV1(message.OrderId, fully, DateTimeOffset.UtcNow));
        await outbox.SaveChangesAndFlushMessagesAsync(cancellationToken);
    }

    private async Task<decimal> AvailableToPromiseAsync(Sku sku, WarehouseCode warehouse, CancellationToken ct)
    {
        var items = await stockItems.ListBySkuAsync(sku, warehouse, ct);
        var available = items.Sum(i => i.Available.Amount);
        var outstanding = await reservations.ListOutstandingAsync(sku, warehouse, ct);
        var promised = outstanding.Sum(r => r.Outstanding.Amount);
        return Math.Max(0m, available - promised);
    }
}

/// <summary>Releases an order's soft reservations when it is cancelled (frees available-to-promise).</summary>
public sealed class ReleaseReservationsConsumer(IStockReservationRepository reservations, IUnitOfWork unitOfWork)
{
    public async Task Handle(OutboundOrderCancelledV1 message, CancellationToken cancellationToken)
    {
        var orderRef = OrderRef.Of(message.OrderId.ToString());
        var list = await reservations.ListByOrderAsync(orderRef, cancellationToken);

        var changed = false;
        foreach (var reservation in list)
        {
            if (reservation.Status is ReservationStatus.Open or ReservationStatus.PartiallyAllocated)
            {
                reservation.Release();
                reservations.Update(reservation);
                changed = true;
            }
        }

        if (changed)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}

/// <summary>
/// UC-12 (Inventory side): on <see cref="ShipmentDispatchedV1"/>, deducts the dispatched stock. The
/// FEFO wave/pick optimiser is deferred (docs/PLAN.md), so this collapses pick+dispatch: for each
/// outstanding reservation it allocates and picks across the SKU's available stock (simplest order),
/// emitting one <c>Pick</c> ledger entry per stock item so on-hand actually drops.
/// </summary>
public sealed class DispatchConsumer(
    IStockItemRepository stockItems,
    IStockReservationRepository reservations,
    IStockLedger ledger,
    IUnitOfWork unitOfWork)
{
    public async Task Handle(ShipmentDispatchedV1 message, CancellationToken cancellationToken)
    {
        var warehouse = WarehouseCode.Of(message.WarehouseCode);
        var orderRef = OrderRef.Of(message.OrderId.ToString());
        var list = await reservations.ListByOrderAsync(orderRef, cancellationToken);

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
                var allocation = item.Allocate(qty, orderRef, reservation.Id);
                reservation.RecordAllocation(qty);
                ledger.Append(item.Pick(allocation.Id, qty, performedBy: "dispatch"));
                stockItems.Update(item);
                remaining -= take;
            }

            reservations.Update(reservation);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
