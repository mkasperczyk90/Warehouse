using Microsoft.Extensions.DependencyInjection;
using Warehouse.SharedKernel.Application;
using Warehouse.Warehousing.Topology.Application.Abstractions;
using Warehouse.Warehousing.Topology.Domain;
using Xunit;

namespace Warehouse.Warehousing.IntegrationTests;

public sealed class WarehouseSitePersistenceTests(TopologyDatabaseFixture fixture)
    : TopologyIntegrationTest(fixture)
{
    private async Task<WarehouseCode> PersistAsync(WarehouseSite site)
    {
        await using var scope = Fixture.NewScope();
        scope.ServiceProvider.GetRequiredService<IWarehouseRepository>().Add(site);
        await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
        return site.Code;
    }

    [Fact]
    public async Task WarehouseSite_round_trips_through_postgres_with_its_owned_rooms_locations_and_docks()
    {
        var code = await PersistAsync(TopologySample.ColdSite());

        await using var scope = Fixture.NewScope();
        var reloaded = await scope.ServiceProvider.GetRequiredService<IWarehouseRepository>().GetByIdAsync(code);

        Assert.NotNull(reloaded);
        Assert.Equal("WAW1", reloaded!.Code.Value);
        Assert.Equal("Warsaw DC", reloaded.Name);
        Assert.Equal("Wrocław", reloaded.Address.City);

        // Owned room with its nested environment + temperature range.
        var room = Assert.Single(reloaded.Rooms);
        Assert.Equal("CHLD1", room.Code.Value);
        Assert.Equal(RoomType.ColdRoom, room.Type);
        Assert.Equal(2, room.Environment.MaintainedTemperature.MinCelsius);
        Assert.Equal(6, room.Environment.MaintainedTemperature.MaxCelsius);

        // Owned location collection under the room.
        var location = Assert.Single(room.Locations);
        Assert.Equal("WAW1-CHLD1-A-01", location.Code.Value);
        Assert.Equal(LocationKind.Rack, location.Kind);
        Assert.Equal(1.5m, location.Capacity.CubicMeters);
        Assert.Equal(500, location.MaxLoad.Kilograms);

        // Owned dock collection under the warehouse.
        var dock = Assert.Single(reloaded.Docks);
        Assert.Equal("D01", dock.Code.Value);
        Assert.Equal(DockDirection.Inbound, dock.Direction);
    }

    [Fact]
    public async Task Adding_a_room_to_a_reloaded_aggregate_persists_on_the_next_save()
    {
        var code = await PersistAsync(TopologySample.ColdSite());

        await using (var scope = Fixture.NewScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IWarehouseRepository>();
            var site = await repo.GetByIdAsync(code);
            site!.AddRoom(RoomCode.Of("FRZ1"), RoomType.Freezer);
            repo.Update(site);
            await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
        }

        await using var verify = Fixture.NewScope();
        var reloaded = await verify.ServiceProvider.GetRequiredService<IWarehouseRepository>().GetByIdAsync(code);
        Assert.Equal(2, reloaded!.Rooms.Count);
        Assert.Contains(reloaded.Rooms, r => r.Code.Value == "FRZ1");
    }
}
