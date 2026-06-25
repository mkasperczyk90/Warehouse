using Microsoft.Extensions.DependencyInjection;
using Warehouse.Contracts.Topology;
using Warehouse.SharedKernel.Application;
using Warehouse.Warehousing.Inventory.Application.Abstractions;
using Warehouse.Warehousing.Inventory.Application.Consumers;
using Warehouse.Warehousing.Inventory.Domain;
using Xunit;

namespace Warehouse.Warehousing.IntegrationTests;

/// <summary>
/// Drives the real <see cref="LocationProjectionConsumer"/> against Postgres through the production DI
/// wiring, so the new <c>warehouse</c>/<c>room</c> columns, the EF mapping and the room query are all
/// exercised end-to-end.
/// </summary>
public sealed class LocationSnapshotProjectionTests(InventoryDatabaseFixture fixture)
    : InventoryIntegrationTest(fixture)
{
    private static LocationProjectionConsumer NewConsumer(AsyncServiceScope scope) => new(
        scope.ServiceProvider.GetRequiredService<ILocationSnapshotRepository>(),
        scope.ServiceProvider.GetRequiredService<IUnitOfWork>());

    [Fact]
    public async Task LocationDefined_then_RoomEnvironmentChanged_round_trips_through_postgres()
    {
        var now = DateTimeOffset.UtcNow;

        // Two locations land in a cold room (0..8°C), each its own LocationDefined event.
        await using (var scope = Fixture.NewScope())
        {
            var consumer = NewConsumer(scope);
            await consumer.Handle(
                new LocationDefinedV1("WAW1", "CHLD1", "WAW1-CHLD1-A-01", "Rack", 1.5m, 500, 0, 8, false, now),
                CancellationToken.None);
            await consumer.Handle(
                new LocationDefinedV1("WAW1", "CHLD1", "WAW1-CHLD1-A-02", "Rack", 2.0m, 600, 0, 8, false, now),
                CancellationToken.None);
        }

        // The room is re-tuned to 2..6°C; both locations in it should pick up the new range.
        await using (var scope = Fixture.NewScope())
        {
            await NewConsumer(scope).Handle(
                new RoomEnvironmentChangedV1("WAW1", "CHLD1", 2, 6, now.AddMinutes(1)), CancellationToken.None);
        }

        await using var verify = Fixture.NewScope();
        var repo = verify.ServiceProvider.GetRequiredService<ILocationSnapshotRepository>();

        var inRoom = await repo.ListByRoomAsync(WarehouseCode.Of("WAW1"), "CHLD1");
        Assert.Equal(2, inRoom.Count);
        Assert.All(inRoom, s =>
        {
            Assert.Equal(2, s.EnvironmentTemperature.MinCelsius);
            Assert.Equal(6, s.EnvironmentTemperature.MaxCelsius);
        });

        var one = await repo.FindAsync(LocationCode.Of("WAW1-CHLD1-A-01"));
        Assert.NotNull(one);
        Assert.Equal("WAW1", one!.Warehouse.Value);
        Assert.Equal("CHLD1", one.Room);
        Assert.Equal(1.5m, one.Capacity.CubicMeters);  // capacity is the location's, untouched by the room change
        Assert.Equal(500, one.MaxLoad.Kilograms);
    }
}
