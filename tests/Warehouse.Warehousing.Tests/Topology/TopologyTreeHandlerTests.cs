using Warehouse.SharedKernel.ValueObjects;
using Warehouse.Warehousing.Topology.Application.Tree.GetLocations;
using Warehouse.Warehousing.Topology.Application.Tree.GetRoom;
using Warehouse.Warehousing.Topology.Application.Tree.GetTopologyTree;
using Warehouse.Warehousing.Topology.Domain;
using Xunit;

namespace Warehouse.Warehousing.Tests.Topology;

/// <summary>Covers the admin read-model projection (tree + room detail) against in-memory aggregates;
/// the DbContext load itself is exercised by the integration suite (mirrors <c>GetWarehouse</c>).</summary>
public sealed class TopologyTreeHandlerTests
{
    private static (WarehouseSite Site, Room Room) ColdRoomWithLocation()
    {
        var site = Build.Warehouse("WAW1", "Warsaw DC");
        site.AddRoom(RoomCode.Of("CR1"), RoomType.ColdRoom, RoomEnvironment.For(RoomType.ColdRoom, TemperatureRange.Of(2, 6)));
        site.AddLocation(
            RoomCode.Of("CR1"), LocationCode.Of("WAW1-CR1-A01-R1-S1"),
            LocationKind.Rack, Volume.FromCubicMeters(1.2m), Weight.FromKilograms(500m));
        return (site, site.Rooms.Single());
    }

    [Fact]
    public void MapTree_emits_a_warehouse_node_then_its_room_nodes()
    {
        var (site, _) = ColdRoomWithLocation();

        var nodes = GetTopologyTreeHandler.MapTree([site]);

        Assert.Equal(2, nodes.Count);

        var warehouse = nodes[0];
        Assert.Equal("WAW1", warehouse.Id);
        Assert.Equal(1, warehouse.Level);
        Assert.Equal("warehouse", warehouse.Kind);
        Assert.Equal("warehouse", warehouse.Icon);
        Assert.Contains("Warsaw DC", warehouse.Label);

        var room = nodes[1];
        Assert.Equal("WAW1:CR1", room.Id);          // composite id round-trips to the room endpoint
        Assert.Equal(2, room.Level);
        Assert.Equal("room", room.Kind);
        Assert.Equal("cold", room.Icon);
        Assert.Equal("Cold room CR1", room.Label);
        Assert.Matches(@"2.*6 °C", room.Tag!);
    }

    [Fact]
    public void MapRoom_projects_the_room_with_its_locations()
    {
        var (site, room) = ColdRoomWithLocation();

        var dto = GetRoomHandler.MapRoom(site, room);

        Assert.Equal("WAW1:CR1", dto.Id);
        Assert.Equal("Cold room CR1", dto.Name);
        Assert.Equal("WAW1", dto.Warehouse);
        Assert.Equal("cold", dto.Type);
        Assert.Equal(2, dto.TempMin);
        Assert.Equal(6, dto.TempMax);
        Assert.Equal(1, dto.ShownCount);
        Assert.Equal(1, dto.TotalCount);

        var location = Assert.Single(dto.Locations);
        Assert.Equal("WAW1-CR1-A01-R1-S1", location.Address);
        Assert.Equal(1.2m, location.Capacity);
        Assert.Equal(500m, location.LoadLimit);
        Assert.Equal("—", location.Occupied);       // occupancy is Inventory's concern, not Topology's
    }

    [Fact]
    public void MapLocations_flattens_every_location_with_its_room_label()
    {
        var (site, _) = ColdRoomWithLocation();

        var location = Assert.Single(GetLocationsHandler.MapLocations([site]));

        Assert.Equal("WAW1-CR1-A01-R1-S1", location.Code);
        Assert.Equal("Cold room CR1", location.Room);
        Assert.Equal("WAW1", location.Warehouse);
    }
}
