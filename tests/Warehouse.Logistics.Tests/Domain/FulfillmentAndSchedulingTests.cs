using Warehouse.Logistics.Core.Domain;
using Warehouse.Logistics.Core.Domain.Services;
using Warehouse.Logistics.Tests.TestDoubles;
using Warehouse.SharedKernel.ValueObjects;
using Xunit;

namespace Warehouse.Logistics.Tests.Domain;

public sealed class OutboundFulfillmentServiceTests
{
    private static (LocationRef, ProductCode, BatchInfo?, Quantity)[] Picks() =>
        [(LocationRef.Of("L1"), Build.Code(), null, Build.Qty(5))];

    [Fact]
    public void ReleaseToPicking_transitions_the_order_and_builds_a_pick_list()
    {
        var order = Build.ReservedOrder();

        var pickList = OutboundFulfillmentService.ReleaseToPicking(order, Picks());

        Assert.Equal(OrderStatus.Picking, order.Status);
        Assert.Equal(order.Id, pickList.OrderId);
        Assert.Single(pickList.Tasks);
    }

    [Fact]
    public void ReleaseToPicking_refuses_an_order_that_is_not_reserved()
    {
        var order = Build.Order(); // Created

        Expect.DomainError("order_invalid_status",
            () => OutboundFulfillmentService.ReleaseToPicking(order, Picks()));
    }

    [Fact]
    public void CompletePacking_rejects_a_pick_list_from_another_order()
    {
        var order = Build.PickingOrder();
        var foreignPickList = Build.PickListFor(OrderId.New());

        Expect.DomainError("picklist_order_mismatch", () => OutboundFulfillmentService.CompletePacking(
            order, foreignPickList, new PartyRoleRef(Guid.NewGuid().ToString())));
    }

    [Fact]
    public void CompletePacking_rejects_an_incomplete_pick_list()
    {
        var order = Build.PickingOrder();
        var pickList = Build.PickListFor(order.Id); // one pending task

        Expect.DomainError("picklist_incomplete", () => OutboundFulfillmentService.CompletePacking(
            order, pickList, new PartyRoleRef(Guid.NewGuid().ToString())));
    }

    [Fact]
    public void CompletePacking_packs_the_order_and_opens_a_shipment()
    {
        var order = Build.PickingOrder();
        var pickList = Build.PickListFor(order.Id);
        pickList.ConfirmPick(1, "alice");
        var carrier = new PartyRoleRef(Guid.NewGuid().ToString());

        var shipment = OutboundFulfillmentService.CompletePacking(order, pickList, carrier);

        Assert.Equal(OrderStatus.Packed, order.Status);
        Assert.Equal(order.Id, shipment.OrderId);
        Assert.Equal(carrier, shipment.Carrier);
        Assert.Equal(ShipmentStatus.Packing, shipment.Status);
    }
}

public sealed class DockSchedulingServiceTests
{
    private static DockSlot SlotAt(string dock, int fromHour, int toHour)
    {
        var day = new DateTimeOffset(2026, 6, 22, 0, 0, 0, TimeSpan.Zero);
        return DockSlot.Of(dock, day.AddHours(fromHour), day.AddHours(toHour));
    }

    [Fact]
    public void Schedule_books_the_slot_when_the_dock_is_free()
    {
        var delivery = Build.Delivery();
        var slot = SlotAt("D-1", 9, 10);

        DockSchedulingService.Schedule(delivery, slot, []);

        Assert.Equal(slot, delivery.Slot);
    }

    [Fact]
    public void Schedule_rejects_an_overlap_at_the_same_dock()
    {
        var delivery = Build.Delivery();
        var booked = SlotAt("D-1", 9, 11);

        Expect.DomainError("dock_slot_conflict",
            () => DockSchedulingService.Schedule(delivery, SlotAt("D-1", 10, 12), [booked]));
    }

    [Fact]
    public void Schedule_allows_an_overlapping_window_at_a_different_dock()
    {
        var delivery = Build.Delivery();
        var bookedElsewhere = SlotAt("D-2", 9, 11);

        DockSchedulingService.Schedule(delivery, SlotAt("D-1", 10, 12), [bookedElsewhere]);

        Assert.Equal("D-1", delivery.Slot!.DockCode);
    }

    [Fact]
    public void Schedule_allows_back_to_back_slots_that_only_touch_at_the_edge()
    {
        var delivery = Build.Delivery();
        var booked = SlotAt("D-1", 9, 10);

        DockSchedulingService.Schedule(delivery, SlotAt("D-1", 10, 11), [booked]);

        Assert.NotNull(delivery.Slot);
    }
}
