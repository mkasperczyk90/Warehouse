using Warehouse.Contracts.Logistics;
using Warehouse.Warehousing.Inventory.Application.Consumers;
using Warehouse.Warehousing.Inventory.Domain;
using Warehouse.Warehousing.Tests.TestDoubles;
using Xunit;

namespace Warehouse.Warehousing.Tests.Consumers;

public sealed class PickingConsumerTests
{
    // --- PlanPicksConsumer (UC-10, plan) ------------------------------------
    [Fact]
    public async Task PlanPicks_hard_allocates_against_stock_and_announces_the_planned_picks()
    {
        var orderId = Guid.NewGuid();
        var item = Build.Stock(100, location: "L1-A1");
        var stock = new FakeStockItemRepository();
        stock.Seed(item);
        var reservations = new FakeStockReservationRepository();
        var reservation = Build.Reservation(orderId, 30);
        reservations.Seed(reservation);
        var outbox = Outbox.Create();
        var consumer = new PlanPicksConsumer(stock, reservations, outbox);

        await consumer.Handle(new PickingReleasedV1(orderId, Build.Warehouse, DateTimeOffset.UtcNow), CancellationToken.None);

        Assert.Equal(30, item.Allocated.Amount);
        Assert.Equal(ReservationStatus.Allocated, reservation.Status);
        var planned = outbox.PublishedMessage<PicksPlannedV1>();
        var pick = Assert.Single(planned.Picks);
        Assert.Equal("L1-A1", pick.Location);
        Assert.Equal("MILK", pick.Sku);
        Assert.Equal(30, pick.Quantity);
    }

    [Fact]
    public async Task PlanPicks_splits_a_reservation_across_locations_in_location_order()
    {
        var orderId = Guid.NewGuid();
        var near = Build.Stock(20, location: "L1-A1");
        var far = Build.Stock(50, location: "L2-A1");
        var stock = new FakeStockItemRepository();
        stock.Seed(far, near); // seeded out of order on purpose; the consumer sorts by location
        var reservations = new FakeStockReservationRepository();
        var reservation = Build.Reservation(orderId, 30);
        reservations.Seed(reservation);
        var outbox = Outbox.Create();
        var consumer = new PlanPicksConsumer(stock, reservations, outbox);

        await consumer.Handle(new PickingReleasedV1(orderId, Build.Warehouse, DateTimeOffset.UtcNow), CancellationToken.None);

        Assert.Equal(20, near.Allocated.Amount);
        Assert.Equal(10, far.Allocated.Amount);
        Assert.Equal(ReservationStatus.Allocated, reservation.Status);
        var picks = outbox.PublishedMessage<PicksPlannedV1>().Picks;
        Assert.Equal(["L1-A1", "L2-A1"], picks.Select(p => p.Location));
        Assert.Equal([20m, 10m], picks.Select(p => p.Quantity));
    }

    [Fact]
    public async Task PlanPicks_announces_nothing_when_there_is_no_stock_to_allocate()
    {
        var orderId = Guid.NewGuid();
        var stock = new FakeStockItemRepository(); // empty
        var reservations = new FakeStockReservationRepository();
        var reservation = Build.Reservation(orderId, 30);
        reservations.Seed(reservation);
        var outbox = Outbox.Create();
        var consumer = new PlanPicksConsumer(stock, reservations, outbox);

        await consumer.Handle(new PickingReleasedV1(orderId, Build.Warehouse, DateTimeOffset.UtcNow), CancellationToken.None);

        Assert.Empty(outbox.Published());
        Assert.Equal(ReservationStatus.Open, reservation.Status);
    }

    // --- PickStockConsumer (UC-10, pick) ------------------------------------
    private static StockItem AllocatedStock(Guid orderId, decimal onHand, decimal allocate, string location = "L1-A1")
    {
        var item = Build.Stock(onHand, location: location);
        item.Allocate(Build.Qty(allocate), OrderRef.Of(orderId.ToString()), StockReservationId.New());
        return item;
    }

    [Fact]
    public async Task PickStock_consumes_the_active_allocation_and_deducts_on_hand()
    {
        var orderId = Guid.NewGuid();
        var item = AllocatedStock(orderId, onHand: 100, allocate: 30);
        var stock = new FakeStockItemRepository();
        stock.Seed(item);
        var ledger = new FakeStockLedger();
        var uow = new FakeUnitOfWork();
        var consumer = new PickStockConsumer(stock, ledger, uow);

        await consumer.Handle(
            new PickConfirmedV1(orderId, 1, "L1-A1", "MILK", null, 30, "pcs", DateTimeOffset.UtcNow),
            CancellationToken.None);

        Assert.Equal(70, item.OnHand.Amount);
        Assert.Equal(0, item.Allocated.Amount);
        Assert.Single(ledger.Movements);
        Assert.Equal(1, uow.SaveCount);
    }

    [Fact]
    public async Task PickStock_is_a_no_op_when_no_stock_exists_at_the_location()
    {
        var stock = new FakeStockItemRepository(); // empty
        var ledger = new FakeStockLedger();
        var uow = new FakeUnitOfWork();
        var consumer = new PickStockConsumer(stock, ledger, uow);

        await consumer.Handle(
            new PickConfirmedV1(Guid.NewGuid(), 1, "L9-A9", "MILK", null, 30, "pcs", DateTimeOffset.UtcNow),
            CancellationToken.None);

        Assert.Empty(ledger.Movements);
        Assert.Equal(0, uow.SaveCount);
    }

    [Fact]
    public async Task PickStock_is_a_no_op_when_there_is_no_active_allocation_for_the_order()
    {
        var item = Build.Stock(100, location: "L1-A1"); // on hand, but nothing allocated to the order
        var stock = new FakeStockItemRepository();
        stock.Seed(item);
        var ledger = new FakeStockLedger();
        var uow = new FakeUnitOfWork();
        var consumer = new PickStockConsumer(stock, ledger, uow);

        await consumer.Handle(
            new PickConfirmedV1(Guid.NewGuid(), 1, "L1-A1", "MILK", null, 30, "pcs", DateTimeOffset.UtcNow),
            CancellationToken.None);

        Assert.Equal(100, item.OnHand.Amount);
        Assert.Empty(ledger.Movements);
        Assert.Equal(0, uow.SaveCount);
    }
}
