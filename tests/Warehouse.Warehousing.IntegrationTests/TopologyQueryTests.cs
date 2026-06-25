using Microsoft.Extensions.DependencyInjection;
using Warehouse.SharedKernel.Application;
using Warehouse.Warehousing.Topology.Application.Abstractions;
using Warehouse.Warehousing.Topology.Application.Warehouses.GetWarehouse;
using Warehouse.Warehousing.Topology.Application.Warehouses.ListWarehouses;
using Warehouse.Warehousing.Topology.Domain;
using Warehouse.Warehousing.Topology.Infrastructure.Persistence;
using Xunit;

namespace Warehouse.Warehousing.IntegrationTests;

public sealed class TopologyQueryTests(TopologyDatabaseFixture fixture) : TopologyIntegrationTest(fixture)
{
    private async Task SeedAsync(params WarehouseSite[] sites)
    {
        await using var scope = Fixture.NewScope();
        var repo = scope.ServiceProvider.GetRequiredService<IWarehouseRepository>();
        foreach (var site in sites)
        {
            repo.Add(site);
        }

        await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
    }

    [Fact]
    public async Task GetWarehouseHandler_reads_the_full_site_back_as_a_dto()
    {
        await SeedAsync(TopologySample.ColdSite());

        await using var scope = Fixture.NewScope();
        var handler = new GetWarehouseHandler(scope.ServiceProvider.GetRequiredService<TopologyDbContext>());

        var dto = await handler.HandleAsync(new GetWarehouseQuery("WAW1"));

        Assert.NotNull(dto);
        Assert.Equal("WAW1", dto!.Code);
        Assert.Equal("Wrocław", dto.Address.City);
        var room = Assert.Single(dto.Rooms);
        Assert.Equal("CHLD1", room.Code);
        Assert.Equal("ColdRoom", room.Type);
        Assert.Equal(2, room.Environment.MinCelsius);
        Assert.Equal(6, room.Environment.MaxCelsius);
        var location = Assert.Single(room.Locations);
        Assert.Equal("WAW1-CHLD1-A-01", location.Code);
        Assert.Equal("Rack", location.Kind);
        Assert.Equal(1.5m, location.CapacityM3);
        var dock = Assert.Single(dto.Docks);
        Assert.Equal("D01", dock.Code);
        Assert.Equal("Inbound", dock.Direction);
    }

    [Fact]
    public async Task GetWarehouseHandler_returns_null_for_an_unknown_code()
    {
        await using var scope = Fixture.NewScope();
        var handler = new GetWarehouseHandler(scope.ServiceProvider.GetRequiredService<TopologyDbContext>());

        Assert.Null(await handler.HandleAsync(new GetWarehouseQuery("NOPE")));
    }

    [Fact]
    public async Task ListWarehousesHandler_projects_summaries_with_structural_counts()
    {
        await SeedAsync(TopologySample.ColdSite(), TopologySample.AmbientSite());

        await using var scope = Fixture.NewScope();
        var handler = new ListWarehousesHandler(scope.ServiceProvider.GetRequiredService<TopologyDbContext>());

        var all = await handler.HandleAsync(new ListWarehousesQuery());

        Assert.Equal(2, all.Count);

        var cold = all.Single(w => w.Code == "WAW1");
        Assert.Equal("Wrocław", cold.City);
        Assert.Equal(1, cold.RoomCount);
        Assert.Equal(1, cold.DockCount);
        Assert.Equal(1, cold.LocationCount);

        var ambient = all.Single(w => w.Code == "POZ1");
        Assert.Equal(1, ambient.RoomCount);
        Assert.Equal(0, ambient.DockCount);
        Assert.Equal(0, ambient.LocationCount);
    }
}
