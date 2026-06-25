using Warehouse.Contracts.Topology;
using Warehouse.SharedKernel.ValueObjects;
using Warehouse.Warehousing.Inventory.Application.Consumers;
using Warehouse.Warehousing.Inventory.Domain;
using Warehouse.Warehousing.Inventory.Domain.Replicas;
using Warehouse.Warehousing.Tests.TestDoubles;
using Xunit;

namespace Warehouse.Warehousing.Tests.Consumers;

public sealed class LocationProjectionConsumerTests
{
    private static LocationDefinedV1 Defined(
        string location = "WAW1-CHLD1-A-01", decimal min = 0, decimal max = 8, bool hazmat = false) =>
        new("WAW1", "CHLD1", location, "Rack", 1.5m, 500, min, max, hazmat, DateTimeOffset.UtcNow);

    private static LocationSnapshot Snapshot(string location, string room, decimal min, decimal max) =>
        new(LocationCode.Of(location), WarehouseCode.Of("WAW1"), room, TemperatureRange.Of(min, max),
            isHazmatZone: false, Volume.FromCubicMeters(1.5m), Weight.FromKilograms(500), DateTimeOffset.UtcNow);

    [Fact]
    public async Task LocationDefined_inserts_a_new_snapshot()
    {
        var repo = new FakeLocationSnapshotRepository();
        var consumer = new LocationProjectionConsumer(repo, new FakeUnitOfWork());

        await consumer.Handle(Defined(), CancellationToken.None);

        var snap = Assert.Single(repo.All);
        Assert.Equal("WAW1-CHLD1-A-01", snap.Code.Value);
        Assert.Equal("WAW1", snap.Warehouse.Value);
        Assert.Equal("CHLD1", snap.Room);
        Assert.Equal(0, snap.EnvironmentTemperature.MinCelsius);
        Assert.Equal(8, snap.EnvironmentTemperature.MaxCelsius);
        Assert.Equal(1.5m, snap.Capacity.CubicMeters);
        Assert.Equal(500, snap.MaxLoad.Kilograms);
        Assert.False(snap.IsHazmatZone);
    }

    [Fact]
    public async Task LocationDefined_is_idempotent_and_re_projects_an_existing_snapshot()
    {
        var repo = new FakeLocationSnapshotRepository();
        var consumer = new LocationProjectionConsumer(repo, new FakeUnitOfWork());
        await consumer.Handle(Defined(max: 8), CancellationToken.None);

        // A redelivery with a corrected range updates the same row rather than duplicating it.
        await consumer.Handle(Defined(min: 2, max: 6), CancellationToken.None);

        var snap = Assert.Single(repo.All);
        Assert.Equal(2, snap.EnvironmentTemperature.MinCelsius);
        Assert.Equal(6, snap.EnvironmentTemperature.MaxCelsius);
    }

    [Fact]
    public async Task RoomEnvironmentChanged_refreshes_every_location_in_the_room_only()
    {
        var repo = new FakeLocationSnapshotRepository();
        repo.Seed(
            Snapshot("WAW1-CHLD1-A-01", "CHLD1", 0, 8),
            Snapshot("WAW1-CHLD1-A-02", "CHLD1", 0, 8),
            Snapshot("WAW1-STD1-A-01", "STD1", 5, 30));
        var consumer = new LocationProjectionConsumer(repo, new FakeUnitOfWork());

        await consumer.Handle(new RoomEnvironmentChangedV1("WAW1", "CHLD1", 2, 6, DateTimeOffset.UtcNow), CancellationToken.None);

        foreach (var snap in repo.All.Where(s => s.Room == "CHLD1"))
        {
            Assert.Equal(2, snap.EnvironmentTemperature.MinCelsius);
            Assert.Equal(6, snap.EnvironmentTemperature.MaxCelsius);
        }

        // The location in a different room keeps its environment.
        var untouched = repo.All.Single(s => s.Room == "STD1");
        Assert.Equal(5, untouched.EnvironmentTemperature.MinCelsius);
        Assert.Equal(30, untouched.EnvironmentTemperature.MaxCelsius);
    }

    [Fact]
    public async Task RoomEnvironmentChanged_for_a_room_with_no_replicas_is_a_no_op()
    {
        var repo = new FakeLocationSnapshotRepository();
        var consumer = new LocationProjectionConsumer(repo, new FakeUnitOfWork());

        await consumer.Handle(new RoomEnvironmentChangedV1("WAW1", "GHOST", 2, 6, DateTimeOffset.UtcNow), CancellationToken.None);

        Assert.Empty(repo.All);
    }
}
