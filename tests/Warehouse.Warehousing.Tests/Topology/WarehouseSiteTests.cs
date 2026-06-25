using Warehouse.SharedKernel.ValueObjects;
using Warehouse.Warehousing.Topology.Domain;
using Warehouse.Warehousing.Topology.Domain.Events;
using Xunit;

namespace Warehouse.Warehousing.Tests.Topology;

public sealed class WarehouseSiteTests
{
    [Fact]
    public void Establish_trims_the_name()
    {
        var site = WarehouseSite.Establish(WarehouseCode.Of("WAW1"), "  Warsaw DC  ", Build.Address());

        Assert.Equal("Warsaw DC", site.Name);
        Assert.Equal("WAW1", site.Code.Value);
    }

    [Fact]
    public void AddRoom_rejects_a_duplicate_code()
    {
        var site = Build.Warehouse("WAW1");
        site.AddRoom(RoomCode.Of("CHLD1"), RoomType.ColdRoom);

        var ex = Assert.Throws<Warehouse.SharedKernel.Domain.DomainException>(
            () => site.AddRoom(RoomCode.Of("CHLD1"), RoomType.ColdRoom));
        Assert.Equal("room_code_duplicate", ex.ErrorCode);
    }

    [Fact]
    public void AddLocation_raises_LocationDefined_with_the_rooms_environment()
    {
        var site = Build.WarehouseWithRoom("WAW1", "CHLD1");

        site.AddLocation(
            RoomCode.Of("CHLD1"),
            LocationCode.Of("WAW1-CHLD1-A-01"),
            LocationKind.Rack,
            Volume.FromCubicMeters(1.5m),
            Weight.FromKilograms(500));

        var defined = Assert.Single(site.DomainEvents.OfType<LocationDefined>());
        Assert.Equal("WAW1", defined.Warehouse.Value);
        Assert.Equal("CHLD1", defined.Room.Value);
        Assert.Equal("WAW1-CHLD1-A-01", defined.Location.Value);
        Assert.Equal(LocationKind.Rack, defined.Kind);
    }

    [Fact]
    public void ChangeRoomEnvironment_raises_RoomEnvironmentChanged()
    {
        var site = Build.WarehouseWithRoom("WAW1", "CHLD1");

        site.ChangeRoomEnvironment(RoomCode.Of("CHLD1"), RoomEnvironment.For(RoomType.ColdRoom, TemperatureRange.Of(1, 5)));

        var changed = Assert.Single(site.DomainEvents.OfType<RoomEnvironmentChanged>());
        Assert.Equal("CHLD1", changed.Room.Value);
        Assert.Equal(1, changed.Environment.MaintainedTemperature.MinCelsius);
        Assert.Equal(5, changed.Environment.MaintainedTemperature.MaxCelsius);
    }
}
