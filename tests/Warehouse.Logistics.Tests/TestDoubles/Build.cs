using Warehouse.Logistics.Core.Domain;
using Warehouse.SharedKernel.ValueObjects;

namespace Warehouse.Logistics.Tests.TestDoubles;

/// <summary>Builders for the aggregates under test, so each test states only what it cares about.</summary>
internal static class Build
{
    public static readonly UnitOfMeasure Pcs = UnitOfMeasure.Piece;

    public static Quantity Qty(decimal amount) => Quantity.Of(amount, Pcs);

    public static ProductCode Code(string value = "SKU-1") => ProductCode.Of(value);

    public static Address ShipTo() => Address.Of("Main 1", "Wrocław", "50-001", "PL");

    /// <summary>A freshly created order (status <c>Created</c>) with one line.</summary>
    public static OutboundOrder Order(decimal quantity = 10, string product = "SKU-1") =>
        OutboundOrder.Create(
            new PartyRoleRef(Guid.NewGuid()),
            ShipTo(),
            WarehouseRef.Of("WH01"),
            DateTimeOffset.UtcNow.AddDays(2),
            [(Code(product), Qty(quantity))]);

    /// <summary>An order advanced to <c>Reserved</c> (the state a wave is released from).</summary>
    public static OutboundOrder ReservedOrder()
    {
        var order = Order();
        order.MarkReserved(fully: true);
        return order;
    }

    /// <summary>An order in <c>Picking</c> (the state packing closes).</summary>
    public static OutboundOrder PickingOrder()
    {
        var order = ReservedOrder();
        order.StartPicking();
        return order;
    }

    /// <summary>An order in <c>Packed</c> (the state dispatch consumes).</summary>
    public static OutboundOrder PackedOrder()
    {
        var order = PickingOrder();
        order.MarkPacked();
        return order;
    }

    /// <summary>A pick list for <paramref name="orderId"/> with a single pending task at sequence 1.</summary>
    public static PickList PickListFor(OrderId orderId, BatchInfo? batch = null) =>
        PickList.CreateFor(
            orderId,
            [(LocationRef.Of("WH01-A1-R1-S1"), Code(), batch, Qty(5))]);

    /// <summary>A delivery (status <c>Announced</c>) with one line.</summary>
    public static InboundDelivery Delivery(string product = "SKU-1") =>
        InboundDelivery.Announce(
            new PartyRoleRef(Guid.NewGuid()),
            WarehouseRef.Of("WH01"),
            DateTimeOffset.UtcNow.AddDays(1),
            [(Code(product), Qty(100), null)]);

    /// <summary>A delivery moved to <c>Receiving</c> (the state a receipt is recorded in).</summary>
    public static InboundDelivery ReceivingDelivery(string product = "SKU-1")
    {
        var delivery = Delivery(product);
        delivery.RegisterArrival();
        delivery.StartReceiving();
        return delivery;
    }
}
