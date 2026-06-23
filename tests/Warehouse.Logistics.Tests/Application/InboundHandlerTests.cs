using Warehouse.Contracts.Logistics;
using Warehouse.Logistics.Core.Application.Deliveries.AnnounceDelivery;
using Warehouse.Logistics.Core.Application.Deliveries.AssignDockSlot;
using Warehouse.Logistics.Core.Application.Deliveries.CancelDelivery;
using Warehouse.Logistics.Core.Application.Deliveries.ConfirmReceipt;
using Warehouse.Logistics.Core.Application.Deliveries.RecordReceiptLine;
using Warehouse.Logistics.Core.Application.Deliveries.RegisterArrival;
using Warehouse.Logistics.Core.Application.Deliveries.StartReceiving;
using Warehouse.Logistics.Core.Domain;
using Warehouse.Logistics.Tests.TestDoubles;
using Xunit;

namespace Warehouse.Logistics.Tests.Application;

public sealed class InboundHandlerTests
{
    // --- AnnounceDelivery (UC-01) -------------------------------------------
    private static AnnounceDeliveryCommand AnnounceCommand(string sku = "SKU-1") => new(
        Guid.NewGuid(),
        "WH01",
        DateTimeOffset.UtcNow.AddDays(1),
        [new AnnounceDeliveryLine(sku, 100, "pcs")]);

    [Fact]
    public async Task AnnounceDelivery_persists_the_asn_when_the_skus_are_known()
    {
        var deliveries = new FakeInboundDeliveryRepository();
        var catalog = new FakeCatalogProductReplica();
        catalog.Seed("SKU-1");
        var uow = new FakeUnitOfWork();
        var handler = new AnnounceDeliveryHandler(deliveries, catalog, uow);

        var id = await handler.HandleAsync(AnnounceCommand());

        var saved = Assert.Single(deliveries.Saved);
        Assert.Equal(id, saved.Id.Value);
        Assert.Equal(DeliveryStatus.Announced, saved.Status);
        Assert.Equal(1, uow.SaveCount);
    }

    [Fact]
    public async Task AnnounceDelivery_rejects_an_unknown_sku_before_it_enters_receiving()
    {
        var deliveries = new FakeInboundDeliveryRepository();
        var handler = new AnnounceDeliveryHandler(deliveries, new FakeCatalogProductReplica(), new FakeUnitOfWork());

        await Expect.DomainErrorAsync("delivery_unknown_sku", () => handler.HandleAsync(AnnounceCommand()));
        Assert.Empty(deliveries.Saved);
    }

    // --- AssignDockSlot (UC-01) ---------------------------------------------
    [Fact]
    public async Task AssignDockSlot_books_the_window_for_an_announced_delivery()
    {
        var deliveries = new FakeInboundDeliveryRepository();
        var delivery = Build.Delivery();
        deliveries.Seed(delivery);
        var uow = new FakeUnitOfWork();
        var handler = new AssignDockSlotHandler(deliveries, uow);
        var from = DateTimeOffset.UtcNow.AddHours(2);

        await handler.HandleAsync(new AssignDockSlotCommand(delivery.Id.Value, "D-1", from, from.AddHours(1)));

        Assert.NotNull(delivery.Slot);
        Assert.Equal("D-1", delivery.Slot!.DockCode);
    }

    [Fact]
    public async Task AssignDockSlot_rejects_a_window_that_clashes_at_the_same_dock()
    {
        var deliveries = new FakeInboundDeliveryRepository();
        var delivery = Build.Delivery();
        deliveries.Seed(delivery);
        var from = DateTimeOffset.UtcNow.AddHours(2);
        deliveries.BookedSlots.Add(DockSlot.Of("D-1", from.AddMinutes(30), from.AddHours(2)));
        var handler = new AssignDockSlotHandler(deliveries, new FakeUnitOfWork());

        await Expect.DomainErrorAsync("dock_slot_conflict",
            () => handler.HandleAsync(new AssignDockSlotCommand(delivery.Id.Value, "D-1", from, from.AddHours(1))));
    }

    // --- RegisterArrival / StartReceiving (UC-02) ---------------------------
    [Fact]
    public async Task RegisterArrival_then_StartReceiving_walks_the_lifecycle()
    {
        var deliveries = new FakeInboundDeliveryRepository();
        var delivery = Build.Delivery();
        deliveries.Seed(delivery);

        await new RegisterArrivalHandler(deliveries, new FakeUnitOfWork())
            .HandleAsync(new RegisterArrivalCommand(delivery.Id.Value));
        Assert.Equal(DeliveryStatus.Arrived, delivery.Status);

        await new StartReceivingHandler(deliveries, new FakeUnitOfWork())
            .HandleAsync(new StartReceivingCommand(delivery.Id.Value));
        Assert.Equal(DeliveryStatus.Receiving, delivery.Status);
    }

    [Fact]
    public async Task RegisterArrival_for_a_missing_delivery_throws()
    {
        var handler = new RegisterArrivalHandler(new FakeInboundDeliveryRepository(), new FakeUnitOfWork());

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => handler.HandleAsync(new RegisterArrivalCommand(Guid.NewGuid())));
    }

    // --- RecordReceiptLine (UC-02) ------------------------------------------
    [Fact]
    public async Task RecordReceiptLine_records_the_counted_quantity_batch_and_discrepancy()
    {
        var deliveries = new FakeInboundDeliveryRepository();
        var delivery = Build.ReceivingDelivery();
        deliveries.Seed(delivery);
        var uow = new FakeUnitOfWork();
        var handler = new RecordReceiptLineHandler(deliveries, uow);

        await handler.HandleAsync(new RecordReceiptLineCommand(
            delivery.Id.Value, 1, 90, "pcs", "LOT-7", new DateOnly(2026, 12, 1), "Shortage", "two cases missing"));

        var line = delivery.Lines.Single(l => l.LineNo == 1);
        Assert.True(line.IsRecorded);
        Assert.Equal(90, line.Actual!.Amount);
        Assert.Equal("LOT-7", line.Batch!.Number);
        Assert.Equal(DiscrepancyType.Shortage, line.Discrepancy);
        Assert.Equal(1, uow.SaveCount);
    }

    // --- ConfirmReceipt (UC-02) ---------------------------------------------
    [Fact]
    public async Task ConfirmReceipt_confirms_and_announces_received_goods_to_inventory()
    {
        var deliveries = new FakeInboundDeliveryRepository();
        var delivery = Build.ReceivingDelivery();
        delivery.RecordReceipt(1, Build.Qty(100), BatchInfo.Of("LOT-1"), DiscrepancyType.None);
        deliveries.Seed(delivery);
        var outbox = Outbox.Create();
        var handler = new ConfirmReceiptHandler(deliveries, outbox);

        await handler.HandleAsync(new ConfirmReceiptCommand(delivery.Id.Value));

        Assert.Equal(DeliveryStatus.Received, delivery.Status);
        var confirmed = outbox.PublishedMessage<GoodsReceiptConfirmedV1>();
        Assert.Equal(delivery.Id.Value, confirmed.DeliveryId);
        Assert.Single(confirmed.Lines);
    }

    // --- CancelDelivery -----------------------------------------------------
    [Fact]
    public async Task CancelDelivery_cancels_an_announced_delivery()
    {
        var deliveries = new FakeInboundDeliveryRepository();
        var delivery = Build.Delivery();
        deliveries.Seed(delivery);
        var uow = new FakeUnitOfWork();
        var handler = new CancelDeliveryHandler(deliveries, uow);

        await handler.HandleAsync(new CancelDeliveryCommand(delivery.Id.Value));

        Assert.Equal(DeliveryStatus.Cancelled, delivery.Status);
        Assert.Equal(1, uow.SaveCount);
    }
}
