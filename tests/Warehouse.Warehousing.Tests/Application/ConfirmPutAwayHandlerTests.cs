using Warehouse.SharedKernel.Domain;
using Warehouse.SharedKernel.ValueObjects;
using Warehouse.Warehousing.Inventory.Application.ConfirmPutAway;
using Warehouse.Warehousing.Inventory.Application.Storage;
using Warehouse.Warehousing.Inventory.Domain;
using Warehouse.Warehousing.Inventory.Domain.Replicas;
using Warehouse.Warehousing.Tests.TestDoubles;
using Xunit;

namespace Warehouse.Warehousing.Tests.Application;

/// <summary>
/// Put-away enforces the hard storage-compatibility invariant against the Topology/Catalog replicas
/// (ADR-0003) before any stock moves. These pin that wiring: hazmat goes only to hazmat zones, an
/// unknown location is not a valid target, and a compatible put-away posts one ledger move.
/// </summary>
public sealed class ConfirmPutAwayHandlerTests
{
    private const string Warehouse = "WH01";
    private static string Buffer => $"{Warehouse}-DOCK-BUFFER";

    private static ProductSnapshot Product(string sku, bool hazardous, TemperatureRange? required = null) =>
        new(Sku.Of(sku), sku, UnitOfMeasure.Piece, Weight.Zero, Volume.Zero, required,
            requiresColdChain: required is not null, isHazardous: hazardous, isBatchTracked: false,
            hasExpiryDate: false, DateTimeOffset.UtcNow);

    private static LocationSnapshot Location(string code, string room, bool hazmatZone, TemperatureRange? temp = null) =>
        new(LocationCode.Of(code), WarehouseCode.Of(Warehouse), room, temp ?? TemperatureRange.Ambient,
            hazmatZone, Volume.FromCubicMeters(10), Weight.FromKilograms(1000), DateTimeOffset.UtcNow);

    private static ConfirmPutAwayCommand Command(string sku, string toLocation, decimal qty = 5) =>
        new(Guid.NewGuid(), Warehouse, sku, BatchNumber: null, qty, "pcs", toLocation, "op-1");

    private static ConfirmPutAwayHandler Handler(
        FakeStockItemRepository stock, FakeProductSnapshotRepository products, FakeLocationSnapshotRepository locations) =>
        new(stock, new StorageCompatibility(stock, products, locations), new FakeStockLedger(), Outbox.Create());

    [Fact]
    public async Task Hazardous_stock_cannot_be_put_away_to_a_non_hazmat_location()
    {
        var stock = new FakeStockItemRepository();
        stock.Seed(Build.Stock(10, sku: "ACID", location: Buffer));
        var products = new FakeProductSnapshotRepository();
        products.Seed(Product("ACID", hazardous: true));
        var locations = new FakeLocationSnapshotRepository();
        locations.Seed(Location($"{Warehouse}-STD1-A-01", "STD1", hazmatZone: false));

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => Handler(stock, products, locations).HandleAsync(Command("ACID", $"{Warehouse}-STD1-A-01")));
        Assert.Equal("put_away_incompatible", ex.ErrorCode);
    }

    [Fact]
    public async Task Hazardous_stock_can_be_put_away_to_a_hazmat_zone()
    {
        var stock = new FakeStockItemRepository();
        stock.Seed(Build.Stock(10, sku: "ACID", location: Buffer));
        var ledger = new FakeStockLedger();
        var products = new FakeProductSnapshotRepository();
        products.Seed(Product("ACID", hazardous: true));
        var locations = new FakeLocationSnapshotRepository();
        locations.Seed(Location($"{Warehouse}-HAZ1-A-01", "HAZ1", hazmatZone: true));
        var handler = new ConfirmPutAwayHandler(
            stock, new StorageCompatibility(stock, products, locations), ledger, Outbox.Create());

        await handler.HandleAsync(Command("ACID", $"{Warehouse}-HAZ1-A-01"));

        var move = Assert.Single(ledger.Movements);
        Assert.Equal(MovementType.PutAway, move.Type);
    }

    [Fact]
    public async Task Cold_chain_stock_cannot_be_put_away_to_a_warm_location()
    {
        var stock = new FakeStockItemRepository();
        stock.Seed(Build.Stock(10, sku: "MILK", location: Buffer));
        var products = new FakeProductSnapshotRepository();
        products.Seed(Product("MILK", hazardous: false, required: TemperatureRange.Of(2, 6)));
        var locations = new FakeLocationSnapshotRepository();
        locations.Seed(Location($"{Warehouse}-STD1-A-01", "STD1", hazmatZone: false)); // ambient 5..30 °C

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => Handler(stock, products, locations).HandleAsync(Command("MILK", $"{Warehouse}-STD1-A-01")));
        Assert.Equal("put_away_incompatible", ex.ErrorCode);
    }

    [Fact]
    public async Task Cold_chain_stock_can_be_put_away_to_a_cold_room()
    {
        var stock = new FakeStockItemRepository();
        stock.Seed(Build.Stock(10, sku: "MILK", location: Buffer));
        var ledger = new FakeStockLedger();
        var products = new FakeProductSnapshotRepository();
        products.Seed(Product("MILK", hazardous: false, required: TemperatureRange.Of(2, 6)));
        var locations = new FakeLocationSnapshotRepository();
        locations.Seed(Location($"{Warehouse}-CHLD1-A-01", "CHLD1", hazmatZone: false, temp: TemperatureRange.Of(2, 6)));
        var handler = new ConfirmPutAwayHandler(
            stock, new StorageCompatibility(stock, products, locations), ledger, Outbox.Create());

        await handler.HandleAsync(Command("MILK", $"{Warehouse}-CHLD1-A-01"));

        Assert.Single(ledger.Movements);
    }

    [Fact]
    public async Task A_location_unknown_to_topology_is_rejected()
    {
        var stock = new FakeStockItemRepository();
        stock.Seed(Build.Stock(10, sku: "MILK", location: Buffer));
        var products = new FakeProductSnapshotRepository();
        products.Seed(Product("MILK", hazardous: false));
        var locations = new FakeLocationSnapshotRepository(); // target not announced by Topology

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => Handler(stock, products, locations).HandleAsync(Command("MILK", $"{Warehouse}-GHOST-A-01")));
        Assert.Equal("put_away_location_unknown", ex.ErrorCode);
    }

    [Fact]
    public async Task A_compatible_put_away_posts_one_ledger_move()
    {
        var stock = new FakeStockItemRepository();
        stock.Seed(Build.Stock(10, sku: "MILK", location: Buffer));
        var ledger = new FakeStockLedger();
        var products = new FakeProductSnapshotRepository();
        products.Seed(Product("MILK", hazardous: false));
        var locations = new FakeLocationSnapshotRepository();
        locations.Seed(Location($"{Warehouse}-STD1-A-01", "STD1", hazmatZone: false));
        var handler = new ConfirmPutAwayHandler(
            stock, new StorageCompatibility(stock, products, locations), ledger, Outbox.Create());

        await handler.HandleAsync(Command("MILK", $"{Warehouse}-STD1-A-01"));

        var move = Assert.Single(ledger.Movements);
        Assert.Equal(MovementType.PutAway, move.Type);
        Assert.Equal(5, move.Quantity.Amount);
    }
}
