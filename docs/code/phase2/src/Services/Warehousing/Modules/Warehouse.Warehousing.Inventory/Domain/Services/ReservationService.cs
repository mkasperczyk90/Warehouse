using Warehouse.SharedKernel.Domain;
using Warehouse.SharedKernel.ValueObjects;

namespace Warehouse.Warehousing.Inventory.Domain.Services;

/// <summary>
/// Opens a soft reservation only if there is enough available-to-promise (ATP) for the SKU
/// in the warehouse. ATP spans every StockItem of the SKU, so it cannot be computed inside a
/// single aggregate — the application layer passes it in (the same pattern as
/// <see cref="PutAwayPolicy"/> receiving used volume/weight):
/// <c>ATP = Σ OnHand(available stock) − Σ Allocated − Σ outstanding soft reservations</c>.
/// </summary>
public static class ReservationService
{
    public static StockReservation Reserve(
        Sku sku,
        WarehouseCode warehouse,
        OrderRef orderRef,
        Quantity quantity,
        Quantity availableToPromise)
    {
        ArgumentNullException.ThrowIfNull(quantity);
        ArgumentNullException.ThrowIfNull(availableToPromise);

        if (!availableToPromise.IsGreaterThanOrEqualTo(quantity))
        {
            throw new DomainException(
                "stock_insufficient",
                $"Cannot reserve {quantity} of {sku} in {warehouse}: only {availableToPromise} available to promise.");
        }

        return StockReservation.Open(sku, warehouse, orderRef, quantity);
    }
}
