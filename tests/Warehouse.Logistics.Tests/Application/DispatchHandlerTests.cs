using Warehouse.Contracts.Logistics;
using Warehouse.Logistics.Core.Application.Dispatch.AdvanceShipment;
using Warehouse.Logistics.Core.Application.Dispatch.AssignCarrier;
using Warehouse.Logistics.Core.Application.Dispatch.GetDispatchBoard;
using Warehouse.Logistics.Core.Domain;
using Warehouse.Logistics.Tests.TestDoubles;
using Warehouse.SharedKernel.ValueObjects;
using Xunit;

namespace Warehouse.Logistics.Tests.Application;

public sealed class DispatchHandlerTests
{
    private static Shipment Packed(OrderId orderId)
    {
        var shipment = Shipment.CreateAwaitingCarrier(orderId);
        shipment.AddPackage(Weight.FromKilograms(10), PackageDimensions.Of(60, 40, 40));
        return shipment;
    }

    // --- GetDispatchBoard (the projection) ----------------------------------
    [Fact]
    public void Board_groups_shipments_into_lifecycle_columns()
    {
        var order = Build.PackedOrder();
        var awaiting = Packed(order.Id);

        var assigned = Packed(order.Id);
        assigned.AssignCarrier(new PartyRoleRef("DH"), "Tue 14:00");

        var notice = Packed(order.Id);
        notice.AssignCarrier(new PartyRoleRef("GL"), "Wed 09:00");
        notice.SendPickupNotice();

        var dispatched = Packed(order.Id);
        dispatched.AssignCarrier(new PartyRoleRef("DP"), "now");
        dispatched.SendPickupNotice();
        dispatched.AssignTracking(TrackingNumber.Of("1Z-1"));
        dispatched.Dispatch();

        var orders = new Dictionary<OrderId, OutboundOrder> { [order.Id] = order };
        var board = GetDispatchBoardHandler.Map([awaiting, assigned, notice, dispatched], orders);

        Assert.Equal(["awaitingCarrier", "assigned", "noticeSent", "dispatched"], board.Select(c => c.Key));

        var awaitingCard = Assert.Single(board.Single(c => c.Key == "awaitingCarrier").Shipments);
        Assert.True(awaitingCard.CanAssign);
        Assert.Null(awaitingCard.Carrier);
        Assert.Equal(order.Customer.Value, awaitingCard.Customer);
        Assert.Equal("1 pkg · 10 kg", awaitingCard.Summary);

        var assignedCard = Assert.Single(board.Single(c => c.Key == "assigned").Shipments);
        Assert.Equal("DHL", assignedCard.Carrier!.Name);
        Assert.Equal("Tue 14:00", assignedCard.Pickup);

        var noticeCard = Assert.Single(board.Single(c => c.Key == "noticeSent").Shipments);
        Assert.Equal("Awaiting collection", noticeCard.Badge!.Label);

        var dispatchedCard = Assert.Single(board.Single(c => c.Key == "dispatched").Shipments);
        Assert.Equal("1Z-1", dispatchedCard.Tracking);
        Assert.Equal("Collected ✓", dispatchedCard.Badge!.Label);
    }

    // --- AssignCarrier ------------------------------------------------------
    [Fact]
    public async Task AssignCarrier_books_the_carrier_and_pickup()
    {
        var shipments = new FakeShipmentRepository();
        var shipment = Packed(OrderId.New());
        shipments.Add(shipment);
        var handler = new AssignCarrierHandler(shipments, new FakeUnitOfWork());

        await handler.HandleAsync(new AssignCarrierCommand(shipment.Id.Value, "DH", "Tue 14:00"));

        var saved = shipments.Saved.Single();
        Assert.Equal(ShipmentStatus.CarrierAssigned, saved.Status);
        Assert.Equal("Tue 14:00", saved.Pickup);
    }

    // --- AdvanceShipment ----------------------------------------------------
    [Fact]
    public async Task Advance_sends_the_pickup_notice_then_dispatches_with_an_event()
    {
        var order = Build.PackedOrder();
        var orders = new FakeOutboundOrderRepository();
        orders.Seed(order);
        var shipment = Packed(order.Id);
        shipment.AssignCarrier(new PartyRoleRef("DH"), "Tue 14:00");
        var shipments = new FakeShipmentRepository();
        shipments.Add(shipment);
        var outbox = Outbox.Create();
        var handler = new AdvanceShipmentHandler(shipments, orders, outbox);

        await handler.HandleAsync(new AdvanceShipmentCommand(shipment.Id.Value)); // → ReadyForPickup
        Assert.Equal(ShipmentStatus.ReadyForPickup, shipments.Saved.Single().Status);

        await handler.HandleAsync(new AdvanceShipmentCommand(shipment.Id.Value)); // → Dispatched
        Assert.Equal(ShipmentStatus.Dispatched, shipments.Saved.Single().Status);
        Assert.Equal(OrderStatus.Dispatched, order.Status);

        var dispatched = outbox.PublishedMessage<ShipmentDispatchedV1>();
        Assert.Equal("DH", dispatched.CarrierRoleId);
        Assert.False(string.IsNullOrWhiteSpace(dispatched.TrackingNumber));
    }
}
