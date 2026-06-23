using Warehouse.SharedKernel.ValueObjects;
using Warehouse.Warehousing.Inventory.Domain;
using Warehouse.Warehousing.Inventory.Domain.Services;

namespace Warehouse.Warehousing.Tests.TestDoubles;

/// <summary>Builders for the Inventory aggregates the consumers operate on.</summary>
internal static class Build
{
    public const string Warehouse = "WH01";
    public static readonly UnitOfMeasure Pcs = UnitOfMeasure.Piece;

    public static Quantity Qty(decimal amount) => Quantity.Of(amount, Pcs);

    /// <summary>A stock item with <paramref name="available"/> units on hand (Available == OnHand,
    /// nothing allocated yet).</summary>
    public static StockItem Stock(decimal available, string sku = "MILK", string location = "L1-A1", BatchNumber? batch = null)
    {
        var item = StockItem.CreateAt(LocationCode.Of(location), Sku.Of(sku), batch, Pcs);
        if (available > 0)
        {
            item.Receive(Qty(available), MovementType.GoodsReceipt, "seed");
        }

        return item;
    }

    /// <summary>An open soft reservation for <paramref name="orderId"/> (the ATP gate is satisfied by
    /// passing the promised quantity as available-to-promise).</summary>
    public static StockReservation Reservation(Guid orderId, decimal quantity, string sku = "MILK")
    {
        var qty = Qty(quantity);
        return ReservationService.Reserve(
            Sku.Of(sku), WarehouseCode.Of(Warehouse), OrderRef.Of(orderId.ToString()), qty, qty);
    }
}
