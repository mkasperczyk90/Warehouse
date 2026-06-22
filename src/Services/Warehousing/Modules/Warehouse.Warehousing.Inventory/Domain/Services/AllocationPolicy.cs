using Warehouse.SharedKernel.Domain;
using Warehouse.SharedKernel.ValueObjects;

namespace Warehouse.Warehousing.Inventory.Domain.Services;

/// <summary>
/// Hard-allocation gate, run at wave/pick time (not at order time). It enforces the
/// cross-aggregate rule "a batch on QC hold or expired is invisible to allocation" — and
/// because it runs when the order is released to the floor, a pallet that was QC-blocked or
/// expired <i>after</i> the soft reservation was taken is caught here, before any forklift
/// moves. StockItem and Batch are separate aggregates, so neither can enforce this alone.
///
/// FEFO ordering across candidate stock items (which batch to pick first) is a wave-planning
/// concern handled by the application layer; this policy enforces the per-item rule once a
/// concrete StockItem + Batch has been chosen.
/// </summary>
public static class AllocationPolicy
{
    public static Allocation Allocate(
        StockItem stock,
        Batch? batch,
        Quantity quantity,
        OrderRef orderRef,
        StockReservationId reservationId,
        DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(stock);

        if (stock.Batch is not null)
        {
            if (batch is null)
            {
                throw new DomainException(
                    "allocation_batch_required",
                    $"Stock {stock.Sku} at {stock.Location} is batch-tracked — the batch must be provided.");
            }

            if (batch.Sku != stock.Sku || batch.Number != stock.Batch)
            {
                throw new DomainException(
                    "allocation_batch_mismatch",
                    $"Batch {batch.Number} ({batch.Sku}) does not match stock {stock.Batch} ({stock.Sku}).");
            }

            if (batch.Quality != QualityStatus.Released)
            {
                throw new DomainException(
                    "allocation_batch_not_released",
                    $"Batch {batch.Number} is {batch.Quality} and cannot be allocated.");
            }

            if (batch.IsExpiredAt(today))
            {
                throw new DomainException(
                    "allocation_batch_expired",
                    $"Batch {batch.Number} expired on {batch.ExpiryDate:yyyy-MM-dd}.");
            }
        }

        return stock.Allocate(quantity, orderRef, reservationId);
    }
}
