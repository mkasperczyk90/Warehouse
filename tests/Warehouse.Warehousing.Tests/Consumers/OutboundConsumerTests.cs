using Warehouse.Contracts.Logistics;
using Warehouse.Warehousing.Inventory.Application.Consumers;
using Warehouse.Warehousing.Inventory.Domain;
using Warehouse.Warehousing.Tests.TestDoubles;
using Xunit;

namespace Warehouse.Warehousing.Tests.Consumers;

public sealed class OutboundConsumerTests
{
    private static OutboundOrderPlacedV1 Placed(Guid orderId, decimal quantity, string sku = "MILK") => new(
        orderId, Guid.NewGuid(), Build.Warehouse,
        [new OutboundOrderLineV1(1, sku, quantity, "pcs")], DateTimeOffset.UtcNow);

    // --- ReserveStockConsumer (UC-09, Inventory side) -----------------------
    [Fact]
    public async Task ReserveStock_fully_promises_a_line_when_atp_is_sufficient()
    {
        var orderId = Guid.NewGuid();
        var stock = new FakeStockItemRepository();
        stock.Seed(Build.Stock(100));
        var reservations = new FakeStockReservationRepository();
        var outbox = Outbox.Create();
        var consumer = new ReserveStockConsumer(stock, reservations, outbox);

        await consumer.Handle(Placed(orderId, 30), CancellationToken.None);

        var reservation = Assert.Single(reservations.All);
        Assert.Equal(30, reservation.Quantity.Amount);
        Assert.True(outbox.PublishedMessage<StockReservedV1>().Fully);
    }

    [Fact]
    public async Task ReserveStock_partially_promises_when_atp_is_short()
    {
        var orderId = Guid.NewGuid();
        var stock = new FakeStockItemRepository();
        stock.Seed(Build.Stock(20));
        var reservations = new FakeStockReservationRepository();
        var outbox = Outbox.Create();
        var consumer = new ReserveStockConsumer(stock, reservations, outbox);

        await consumer.Handle(Placed(orderId, 30), CancellationToken.None);

        Assert.Equal(20, Assert.Single(reservations.All).Quantity.Amount);
        Assert.False(outbox.PublishedMessage<StockReservedV1>().Fully);
    }

    [Fact]
    public async Task ReserveStock_promises_nothing_when_there_is_no_stock()
    {
        var stock = new FakeStockItemRepository();
        var reservations = new FakeStockReservationRepository();
        var outbox = Outbox.Create();
        var consumer = new ReserveStockConsumer(stock, reservations, outbox);

        await consumer.Handle(Placed(Guid.NewGuid(), 30), CancellationToken.None);

        Assert.Empty(reservations.All);
        Assert.False(outbox.PublishedMessage<StockReservedV1>().Fully);
    }

    [Fact]
    public async Task ReserveStock_subtracts_existing_outstanding_reservations_from_atp()
    {
        var orderId = Guid.NewGuid();
        var stock = new FakeStockItemRepository();
        stock.Seed(Build.Stock(100));
        var reservations = new FakeStockReservationRepository();
        reservations.Seed(Build.Reservation(Guid.NewGuid(), 80)); // already promised to another order
        var outbox = Outbox.Create();
        var consumer = new ReserveStockConsumer(stock, reservations, outbox);

        await consumer.Handle(Placed(orderId, 50), CancellationToken.None);

        var ours = reservations.All.Single(r => r.OrderRef == OrderRef.Of(orderId.ToString()));
        Assert.Equal(20, ours.Quantity.Amount); // only ATP (100 − 80) could be promised
        Assert.False(outbox.PublishedMessage<StockReservedV1>().Fully);
    }

    // --- ReleaseReservationsConsumer ----------------------------------------
    [Fact]
    public async Task ReleaseReservations_releases_open_reservations_of_a_cancelled_order()
    {
        var orderId = Guid.NewGuid();
        var reservations = new FakeStockReservationRepository();
        var reservation = Build.Reservation(orderId, 30);
        reservations.Seed(reservation);
        var uow = new FakeUnitOfWork();
        var consumer = new ReleaseReservationsConsumer(reservations, uow);

        await consumer.Handle(new OutboundOrderCancelledV1(orderId, DateTimeOffset.UtcNow), CancellationToken.None);

        Assert.Equal(ReservationStatus.Released, reservation.Status);
        Assert.Equal(1, uow.SaveCount);
    }

    [Fact]
    public async Task ReleaseReservations_leaves_a_fully_allocated_reservation_alone()
    {
        var orderId = Guid.NewGuid();
        var reservations = new FakeStockReservationRepository();
        var reservation = Build.Reservation(orderId, 30);
        reservation.RecordAllocation(Build.Qty(30)); // now Allocated — release is the allocations' job
        reservations.Seed(reservation);
        var uow = new FakeUnitOfWork();
        var consumer = new ReleaseReservationsConsumer(reservations, uow);

        await consumer.Handle(new OutboundOrderCancelledV1(orderId, DateTimeOffset.UtcNow), CancellationToken.None);

        Assert.Equal(ReservationStatus.Allocated, reservation.Status);
        Assert.Equal(0, uow.SaveCount);
    }

    // --- DispatchConsumer (UC-12, Inventory side) ---------------------------
    [Fact]
    public async Task Dispatch_allocates_and_picks_outstanding_stock_dropping_on_hand()
    {
        var orderId = Guid.NewGuid();
        var item = Build.Stock(100);
        var stock = new FakeStockItemRepository();
        stock.Seed(item);
        var reservations = new FakeStockReservationRepository();
        var reservation = Build.Reservation(orderId, 30);
        reservations.Seed(reservation);
        var ledger = new FakeStockLedger();
        var uow = new FakeUnitOfWork();
        var consumer = new DispatchConsumer(stock, reservations, ledger, uow);

        await consumer.Handle(
            new ShipmentDispatchedV1(orderId, Guid.NewGuid(), Build.Warehouse, Guid.NewGuid(), "1Z-9", DateTimeOffset.UtcNow),
            CancellationToken.None);

        Assert.Equal(70, item.OnHand.Amount);          // 100 − 30 picked
        Assert.True(item.Available.IsGreaterThanOrEqualTo(Build.Qty(70))); // nothing left allocated
        Assert.Equal(ReservationStatus.Allocated, reservation.Status);
        Assert.Single(ledger.Movements);
        Assert.Equal(1, uow.SaveCount);
    }

    [Fact]
    public async Task Dispatch_ignores_an_order_with_no_outstanding_reservation()
    {
        var orderId = Guid.NewGuid();
        var item = Build.Stock(100);
        var stock = new FakeStockItemRepository();
        stock.Seed(item);
        var reservations = new FakeStockReservationRepository(); // none for this order
        var ledger = new FakeStockLedger();
        var uow = new FakeUnitOfWork();
        var consumer = new DispatchConsumer(stock, reservations, ledger, uow);

        await consumer.Handle(
            new ShipmentDispatchedV1(orderId, Guid.NewGuid(), Build.Warehouse, Guid.NewGuid(), null, DateTimeOffset.UtcNow),
            CancellationToken.None);

        Assert.Equal(100, item.OnHand.Amount);
        Assert.Empty(ledger.Movements);
    }
}
