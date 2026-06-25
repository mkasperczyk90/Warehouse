using Warehouse.Contracts.Catalog;
using Warehouse.Warehousing.Inventory.Application.Consumers;
using Warehouse.Warehousing.Inventory.Domain;
using Warehouse.Warehousing.Tests.TestDoubles;
using Xunit;

namespace Warehouse.Warehousing.Tests.Consumers;

/// <summary>
/// The Catalog replica feed: <see cref="ProductDefinedV2"/> now carries the unit footprint + temperature
/// range, so the projected <c>ProductSnapshot</c> holds the real data the put-away policy validates.
/// </summary>
public sealed class ProductDefinedConsumerTests
{
    private static ProductDefinedV2 Defined(
        string sku = "MILK", decimal? min = 2, decimal? max = 6, decimal weightKg = 1.03m, decimal volumeM3 = 0.001m) =>
        new(sku, "Milk 1L", "pcs", RequiresColdChain: min is not null, IsHazardous: false, IsBatchTracked: true,
            weightKg, volumeM3, min, max, DateTimeOffset.UtcNow);

    [Fact]
    public async Task Projects_the_footprint_and_required_temperature_into_the_snapshot()
    {
        var products = new FakeProductSnapshotRepository();
        var consumer = new ProductDefinedConsumer(products, new FakeUnitOfWork());

        await consumer.Handle(Defined(), CancellationToken.None);

        var snap = await products.FindAsync(Sku.Of("MILK"));
        Assert.NotNull(snap);
        Assert.Equal(1.03m, snap!.UnitWeight.Kilograms);
        Assert.Equal(0.001m, snap.UnitVolume.CubicMeters);
        Assert.True(snap.RequiresColdChain);
        Assert.NotNull(snap.RequiredTemperature);
        Assert.Equal(2, snap.RequiredTemperature!.MinCelsius);
        Assert.Equal(6, snap.RequiredTemperature.MaxCelsius);
    }

    [Fact]
    public async Task An_ambient_product_has_no_required_temperature()
    {
        var products = new FakeProductSnapshotRepository();
        var consumer = new ProductDefinedConsumer(products, new FakeUnitOfWork());

        await consumer.Handle(Defined("RICE", min: null, max: null), CancellationToken.None);

        var snap = await products.FindAsync(Sku.Of("RICE"));
        Assert.NotNull(snap);
        Assert.Null(snap!.RequiredTemperature);
        Assert.False(snap.RequiresColdChain);
    }

    [Fact]
    public async Task A_redelivery_re_projects_the_same_snapshot_rather_than_duplicating()
    {
        var products = new FakeProductSnapshotRepository();
        var consumer = new ProductDefinedConsumer(products, new FakeUnitOfWork());
        await consumer.Handle(Defined(max: 6), CancellationToken.None);

        await consumer.Handle(Defined(max: 4), CancellationToken.None);

        var snap = Assert.Single(products.All);
        Assert.Equal(4, snap.RequiredTemperature!.MaxCelsius);
    }
}
