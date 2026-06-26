using Warehouse.Contracts.Logistics;
using Warehouse.Logistics.Core.Application.Orders.CancelOrder;
using Warehouse.Logistics.Core.Application.Orders.ConfirmDispatch;
using Warehouse.Logistics.Core.Application.PickLists.ConfirmPick;
using Warehouse.Logistics.Core.Application.Orders.CreateOutboundOrder;
using Warehouse.Logistics.Core.Application.Orders.MarkPacked;
using Warehouse.Logistics.Core.Application.PickLists.ReportShortPick;
using Warehouse.Logistics.Core.Application.Orders.StartPicking;
using Warehouse.Logistics.Core.Domain;
using Warehouse.Logistics.Tests.TestDoubles;
using Warehouse.SharedKernel.Domain;
using Warehouse.SharedKernel.ValueObjects;
using Xunit;

namespace Warehouse.Logistics.Tests.Application;

public sealed class OutboundHandlerTests
{
    // --- CreateOutboundOrder (UC-09) ----------------------------------------
    private static CreateOutboundOrderCommand CreateCommand(string sku = "SKU-1", decimal qty = 10) => new(
        Guid.NewGuid().ToString(),
        new OutboundShipTo("Main 1", "Wrocław", "50-001", "PL"),
        "WH01",
        DateTimeOffset.UtcNow.AddDays(2),
        [new OutboundOrderLineInput(sku, qty, "pcs")]);

    [Fact]
    public async Task CreateOutboundOrder_persists_the_order_and_announces_it()
    {
        var orders = new FakeOutboundOrderRepository();
        var catalog = new FakeCatalogProductReplica();
        catalog.Seed("SKU-1");
        var outbox = Outbox.Create();
        var handler = new CreateOutboundOrderHandler(orders, catalog, outbox);

        var id = await handler.HandleAsync(CreateCommand());

        Assert.NotEqual(Guid.Empty, id);
        var saved = Assert.Single(orders.Saved);
        Assert.Equal(id, saved.Id.Value);
        Assert.Equal(OrderStatus.Created, saved.Status);
        Assert.Equal(id, outbox.PublishedMessage<OutboundOrderPlacedV1>().OrderId);
    }

    [Fact]
    public async Task CreateOutboundOrder_rejects_an_unknown_sku()
    {
        var orders = new FakeOutboundOrderRepository();
        var catalog = new FakeCatalogProductReplica(); // nothing seeded → SKU is unknown
        var outbox = Outbox.Create();
        var handler = new CreateOutboundOrderHandler(orders, catalog, outbox);

        await Expect.DomainErrorAsync("order_unknown_sku", () => handler.HandleAsync(CreateCommand()));
        Assert.Empty(orders.Saved);
        Assert.Empty(outbox.Published());
    }

    [Fact]
    public async Task CreateOutboundOrder_rejects_an_order_without_lines()
    {
        var handler = new CreateOutboundOrderHandler(
            new FakeOutboundOrderRepository(), new FakeCatalogProductReplica(), Outbox.Create());

        var cmd = new CreateOutboundOrderCommand(
            Guid.NewGuid().ToString(), new OutboundShipTo("Main 1", "Wrocław", "50-001", "PL"), "WH01", DateTimeOffset.UtcNow, []);

        await Expect.DomainErrorAsync("order_lines_empty", () => handler.HandleAsync(cmd));
    }

    // --- StartPicking (UC-10) -----------------------------------------------
    [Fact]
    public async Task StartPicking_releases_the_wave_and_announces_it()
    {
        var orders = new FakeOutboundOrderRepository();
        var order = Build.ReservedOrder();
        orders.Seed(order);
        var outbox = Outbox.Create();
        var handler = new StartPickingHandler(orders, outbox);

        await handler.HandleAsync(new StartPickingCommand(order.Id.Value));

        Assert.Equal(OrderStatus.Picking, order.Status);
        Assert.Equal(order.Id.Value, outbox.PublishedMessage<PickingReleasedV1>().OrderId);
    }

    [Fact]
    public async Task StartPicking_for_a_missing_order_throws()
    {
        var handler = new StartPickingHandler(new FakeOutboundOrderRepository(), Outbox.Create());

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => handler.HandleAsync(new StartPickingCommand(Guid.NewGuid())));
    }

    // --- ConfirmPick (UC-10) ------------------------------------------------
    [Fact]
    public async Task ConfirmPick_marks_the_task_and_tells_inventory_to_consume_the_allocation()
    {
        var order = Build.PickingOrder();
        var pickLists = new FakePickListRepository();
        var pickList = Build.PickListFor(order.Id);
        pickLists.Seed(pickList);
        var outbox = Outbox.Create();
        var handler = new ConfirmPickHandler(pickLists, outbox);

        await handler.HandleAsync(new ConfirmPickCommand(order.Id.Value, 1, "alice"));

        Assert.Equal(PickTaskStatus.Picked, pickList.Tasks.Single(t => t.Sequence == 1).Status);
        Assert.Equal(order.Id.Value, outbox.PublishedMessage<PickConfirmedV1>().OrderId);
    }

    [Fact]
    public async Task ConfirmPick_without_a_pick_list_throws()
    {
        var handler = new ConfirmPickHandler(new FakePickListRepository(), Outbox.Create());

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => handler.HandleAsync(new ConfirmPickCommand(Guid.NewGuid(), 1, "alice")));
    }

    // --- ReportShortPick (UC-10 exception) ----------------------------------
    [Fact]
    public async Task ReportShortPick_marks_the_task_short()
    {
        var order = Build.PickingOrder();
        var pickLists = new FakePickListRepository();
        var pickList = Build.PickListFor(order.Id);
        pickLists.Seed(pickList);
        var uow = new FakeUnitOfWork();
        var handler = new ReportShortPickHandler(pickLists, uow);

        await handler.HandleAsync(new ReportShortPickCommand(order.Id.Value, 1, "bob", "shortAtLocation"));

        Assert.Equal(PickTaskStatus.ShortPick, pickList.Tasks.Single(t => t.Sequence == 1).Status);
        Assert.Equal(1, uow.SaveCount);
    }

    // --- MarkPacked (UC-11) -------------------------------------------------
    [Fact]
    public async Task MarkPacked_moves_the_order_to_packed_and_commits()
    {
        var orders = new FakeOutboundOrderRepository();
        var order = Build.PickingOrder();
        orders.Seed(order);
        var shipments = new FakeShipmentRepository();
        var uow = new FakeUnitOfWork();
        var handler = new MarkPackedHandler(orders, shipments, uow);

        await handler.HandleAsync(new MarkPackedCommand(order.Id.Value));

        Assert.Equal(OrderStatus.Packed, order.Status);
        // Packing opens the shipment on the board, awaiting a carrier.
        var shipment = Assert.Single(shipments.Saved);
        Assert.Equal(ShipmentStatus.AwaitingCarrier, shipment.Status);
        Assert.Single(shipment.Packages);
        Assert.Equal(1, uow.SaveCount);
    }

    // --- ConfirmDispatch (UC-12) --------------------------------------------
    [Fact]
    public async Task ConfirmDispatch_opens_and_dispatches_a_shipment_and_announces_it()
    {
        var orders = new FakeOutboundOrderRepository();
        var order = Build.PackedOrder();
        orders.Seed(order);
        var shipments = new FakeShipmentRepository();
        var awaiting = Shipment.CreateAwaitingCarrier(order.Id);   // opened when the order was packed
        awaiting.AddPackage(Weight.FromKilograms(10), PackageDimensions.Of(60, 40, 40));
        shipments.Add(awaiting);
        var outbox = Outbox.Create();
        var handler = new ConfirmDispatchHandler(orders, shipments, outbox);
        var carrier = Guid.NewGuid().ToString();

        await handler.HandleAsync(new ConfirmDispatchCommand(order.Id.Value, carrier, "1Z-42", 12.5m));

        Assert.Equal(OrderStatus.Dispatched, order.Status);
        var shipment = Assert.Single(shipments.Saved);
        Assert.Equal(ShipmentStatus.Dispatched, shipment.Status);
        var dispatched = outbox.PublishedMessage<ShipmentDispatchedV1>();
        Assert.Equal(order.Id.Value, dispatched.OrderId);
        Assert.Equal(carrier, dispatched.CarrierRoleId);
        Assert.Equal("1Z-42", dispatched.TrackingNumber);
    }

    [Fact]
    public async Task ConfirmDispatch_requires_a_shipment_to_dispatch()
    {
        var orders = new FakeOutboundOrderRepository();
        var order = Build.PackedOrder();
        orders.Seed(order);
        // No shipment seeded for the order (e.g. it was never packed through the real flow).
        var handler = new ConfirmDispatchHandler(orders, new FakeShipmentRepository(), Outbox.Create());

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => handler.HandleAsync(new ConfirmDispatchCommand(order.Id.Value, Guid.NewGuid().ToString(), null, 5m)));
    }

    // --- CancelOrder --------------------------------------------------------
    [Fact]
    public async Task CancelOrder_cancels_and_announces_so_inventory_releases_reservations()
    {
        var orders = new FakeOutboundOrderRepository();
        var order = Build.ReservedOrder();
        orders.Seed(order);
        var outbox = Outbox.Create();
        var handler = new CancelOrderHandler(orders, outbox);

        await handler.HandleAsync(new CancelOrderCommand(order.Id.Value));

        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Equal(order.Id.Value, outbox.PublishedMessage<OutboundOrderCancelledV1>().OrderId);
    }
}
