using Warehouse.Contracts.Topology;
using Warehouse.SharedKernel.ValueObjects;
using Warehouse.Warehousing.Topology.Application.Warehouses.AddDock;
using Warehouse.Warehousing.Topology.Application.Warehouses.AddLocation;
using Warehouse.Warehousing.Topology.Application.Warehouses.AddRoom;
using Warehouse.Warehousing.Topology.Application.Warehouses.ChangeLocationCapacity;
using Warehouse.Warehousing.Topology.Application.Warehouses.ChangeRoomEnvironment;
using Warehouse.Warehousing.Topology.Domain;
using Xunit;

namespace Warehouse.Warehousing.Tests.Topology;

public sealed class WarehouseStructureHandlerTests
{
    // --- AddRoom ----------------------------------------------------------------

    [Fact]
    public async Task AddRoom_adds_a_room_with_its_environment()
    {
        var warehouses = new FakeWarehouseRepository();
        warehouses.Seed(Build.Warehouse("WAW1"));
        var unitOfWork = new FakeUnitOfWork();
        var handler = new AddRoomHandler(warehouses, unitOfWork);

        var room = await handler.HandleAsync(
            new AddRoomCommand("WAW1", "CHLD1", "ColdRoom", MinCelsius: 2, MaxCelsius: 6, HumidityControlled: false));

        Assert.Equal("CHLD1", room);
        var added = Assert.Single(warehouses.Saved.Single().Rooms);
        Assert.Equal(RoomType.ColdRoom, added.Type);
        Assert.Equal(2, added.Environment.MaintainedTemperature.MinCelsius);
        Assert.Equal(6, added.Environment.MaintainedTemperature.MaxCelsius);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task AddRoom_falls_back_to_the_room_types_default_range_when_no_bounds_are_given()
    {
        var warehouses = new FakeWarehouseRepository();
        warehouses.Seed(Build.Warehouse("WAW1"));
        var handler = new AddRoomHandler(warehouses, new FakeUnitOfWork());

        await handler.HandleAsync(
            new AddRoomCommand("WAW1", "FRZ1", "Freezer", MinCelsius: null, MaxCelsius: null, HumidityControlled: false));

        var room = Assert.Single(warehouses.Saved.Single().Rooms);
        Assert.Equal(-25, room.Environment.MaintainedTemperature.MinCelsius);
        Assert.Equal(-18, room.Environment.MaintainedTemperature.MaxCelsius);
    }

    [Fact]
    public async Task AddRoom_rejects_an_unknown_room_type()
    {
        var warehouses = new FakeWarehouseRepository();
        warehouses.Seed(Build.Warehouse("WAW1"));
        var handler = new AddRoomHandler(warehouses, new FakeUnitOfWork());

        await Expect.DomainErrorAsync(
            "room_type_unknown",
            () => handler.HandleAsync(new AddRoomCommand("WAW1", "CHLD1", "Nonsense", null, null, false)));
    }

    [Fact]
    public async Task AddRoom_rejects_a_range_incompatible_with_the_room_type()
    {
        var warehouses = new FakeWarehouseRepository();
        warehouses.Seed(Build.Warehouse("WAW1"));
        var handler = new AddRoomHandler(warehouses, new FakeUnitOfWork());

        // A "freezer" maintaining -10..-10°C is warmer than the -15°C ceiling the domain enforces.
        await Expect.DomainErrorAsync(
            "room_environment_invalid",
            () => handler.HandleAsync(new AddRoomCommand("WAW1", "FRZ1", "Freezer", -10, -10, false)));
    }

    [Fact]
    public async Task AddRoom_to_a_missing_warehouse_is_a_not_found()
    {
        var handler = new AddRoomHandler(new FakeWarehouseRepository(), new FakeUnitOfWork());

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => handler.HandleAsync(new AddRoomCommand("NOPE", "CHLD1", "ColdRoom", 2, 6, false)));
    }

    // --- AddLocation ------------------------------------------------------------

    [Fact]
    public async Task AddLocation_adds_a_location_and_publishes_LocationDefined()
    {
        var warehouses = new FakeWarehouseRepository();
        warehouses.Seed(Build.WarehouseWithRoom("WAW1", "CHLD1"));  // cold room, default 0..8°C
        var outbox = TopologyOutbox.Create();
        var handler = new AddLocationHandler(warehouses, outbox);

        var location = await handler.HandleAsync(
            new AddLocationCommand("WAW1", "CHLD1", "WAW1-CHLD1-A-01", "Rack", CapacityM3: 1.5m, MaxLoadKg: 500));

        Assert.Equal("WAW1-CHLD1-A-01", location);
        var room = Assert.Single(warehouses.Saved.Single().Rooms);
        var added = Assert.Single(room.Locations);
        Assert.Equal(LocationKind.Rack, added.Kind);
        Assert.Equal(1.5m, added.Capacity.CubicMeters);
        Assert.Equal(500, added.MaxLoad.Kilograms);

        var published = outbox.PublishedMessage<LocationDefinedV1>();
        Assert.Equal("WAW1", published.Warehouse);
        Assert.Equal("CHLD1", published.Room);
        Assert.Equal("WAW1-CHLD1-A-01", published.Location);
        Assert.Equal("Rack", published.Kind);
        Assert.Equal(1.5m, published.CapacityM3);
        Assert.Equal(500, published.MaxLoadKg);
        Assert.Equal(0, published.MinCelsius);
        Assert.Equal(8, published.MaxCelsius);
        Assert.False(published.IsHazmatZone);
    }

    [Fact]
    public async Task AddLocation_marks_a_hazmat_rooms_location_as_a_hazmat_zone()
    {
        var warehouses = new FakeWarehouseRepository();
        warehouses.Seed(Build.WarehouseWithRoom("WAW1", "HAZ1", RoomType.HazmatZone));
        var outbox = TopologyOutbox.Create();
        var handler = new AddLocationHandler(warehouses, outbox);

        await handler.HandleAsync(new AddLocationCommand("WAW1", "HAZ1", "WAW1-HAZ1-A-01", "Floor", 4, 800));

        Assert.True(outbox.PublishedMessage<LocationDefinedV1>().IsHazmatZone);
    }

    [Fact]
    public async Task AddLocation_rejects_a_zero_capacity()
    {
        var warehouses = new FakeWarehouseRepository();
        warehouses.Seed(Build.WarehouseWithRoom("WAW1", "CHLD1"));
        var handler = new AddLocationHandler(warehouses, TopologyOutbox.Create());

        await Expect.DomainErrorAsync(
            "location_capacity_required",
            () => handler.HandleAsync(new AddLocationCommand("WAW1", "CHLD1", "WAW1-CHLD1-A-01", "Rack", 0, 500)));
    }

    // --- ChangeLocationCapacity -------------------------------------------------

    private static FakeWarehouseRepository WarehouseWithLocation()
    {
        var site = Build.WarehouseWithRoom("WAW1", "CHLD1");
        site.AddLocation(
            RoomCode.Of("CHLD1"), LocationCode.Of("WAW1-CHLD1-A-01"),
            LocationKind.Rack, Volume.FromCubicMeters(1.5m), Weight.FromKilograms(500));
        var warehouses = new FakeWarehouseRepository();
        warehouses.Seed(site);
        return warehouses;
    }

    [Fact]
    public async Task ChangeLocationCapacity_re_rates_the_location_and_re_announces_it()
    {
        var warehouses = WarehouseWithLocation();
        var outbox = TopologyOutbox.Create();
        var handler = new ChangeLocationCapacityHandler(warehouses, outbox);

        await handler.HandleAsync(
            new ChangeLocationCapacityCommand("WAW1", "CHLD1", "WAW1-CHLD1-A-01", CapacityM3: 2.5m, MaxLoadKg: 750));

        var location = warehouses.Saved.Single().Rooms.Single().Locations.Single();
        Assert.Equal(2.5m, location.Capacity.CubicMeters);
        Assert.Equal(750, location.MaxLoad.Kilograms);

        // Re-announced on the same upsert event Inventory's LocationSnapshot consumes.
        var published = outbox.PublishedMessage<LocationDefinedV1>();
        Assert.Equal("WAW1-CHLD1-A-01", published.Location);
        Assert.Equal(2.5m, published.CapacityM3);
        Assert.Equal(750, published.MaxLoadKg);
    }

    [Fact]
    public async Task ChangeLocationCapacity_rejects_a_zero_capacity()
    {
        var handler = new ChangeLocationCapacityHandler(WarehouseWithLocation(), TopologyOutbox.Create());

        await Expect.DomainErrorAsync(
            "location_capacity_required",
            () => handler.HandleAsync(new ChangeLocationCapacityCommand("WAW1", "CHLD1", "WAW1-CHLD1-A-01", 0, 500)));
    }

    [Fact]
    public async Task ChangeLocationCapacity_to_a_missing_location_is_a_domain_error()
    {
        var handler = new ChangeLocationCapacityHandler(WarehouseWithLocation(), TopologyOutbox.Create());

        await Expect.DomainErrorAsync(
            "location_not_found",
            () => handler.HandleAsync(new ChangeLocationCapacityCommand("WAW1", "CHLD1", "NOPE-01", 1m, 100)));
    }

    [Fact]
    public async Task AddLocation_rejects_a_duplicate_code()
    {
        var warehouses = new FakeWarehouseRepository();
        warehouses.Seed(Build.WarehouseWithRoom("WAW1", "CHLD1"));
        var handler = new AddLocationHandler(warehouses, TopologyOutbox.Create());
        await handler.HandleAsync(new AddLocationCommand("WAW1", "CHLD1", "WAW1-CHLD1-A-01", "Rack", 1.5m, 500));

        await Expect.DomainErrorAsync(
            "location_code_duplicate",
            () => handler.HandleAsync(new AddLocationCommand("WAW1", "CHLD1", "WAW1-CHLD1-A-01", "Rack", 1.5m, 500)));
    }

    [Fact]
    public async Task AddLocation_to_a_missing_room_is_a_domain_error()
    {
        var warehouses = new FakeWarehouseRepository();
        warehouses.Seed(Build.Warehouse("WAW1"));
        var handler = new AddLocationHandler(warehouses, TopologyOutbox.Create());

        await Expect.DomainErrorAsync(
            "room_not_found",
            () => handler.HandleAsync(new AddLocationCommand("WAW1", "GHOST", "WAW1-GHOST-A-01", "Rack", 1.5m, 500)));
    }

    // --- AddDock ----------------------------------------------------------------

    [Fact]
    public async Task AddDock_adds_a_dock()
    {
        var warehouses = new FakeWarehouseRepository();
        warehouses.Seed(Build.Warehouse("WAW1"));
        var handler = new AddDockHandler(warehouses, new FakeUnitOfWork());

        var dock = await handler.HandleAsync(new AddDockCommand("WAW1", "D01", "Inbound"));

        Assert.Equal("D01", dock);
        var added = Assert.Single(warehouses.Saved.Single().Docks);
        Assert.Equal(DockDirection.Inbound, added.Direction);
    }

    [Fact]
    public async Task AddDock_rejects_an_unknown_direction()
    {
        var warehouses = new FakeWarehouseRepository();
        warehouses.Seed(Build.Warehouse("WAW1"));
        var handler = new AddDockHandler(warehouses, new FakeUnitOfWork());

        await Expect.DomainErrorAsync(
            "dock_direction_unknown",
            () => handler.HandleAsync(new AddDockCommand("WAW1", "D01", "Sideways")));
    }

    // --- ChangeRoomEnvironment --------------------------------------------------

    [Fact]
    public async Task ChangeRoomEnvironment_updates_the_range_and_publishes_RoomEnvironmentChanged()
    {
        var warehouses = new FakeWarehouseRepository();
        warehouses.Seed(Build.WarehouseWithRoom("WAW1", "CHLD1"));
        var outbox = TopologyOutbox.Create();
        var handler = new ChangeRoomEnvironmentHandler(warehouses, outbox);

        await handler.HandleAsync(
            new ChangeRoomEnvironmentCommand("WAW1", "CHLD1", MinCelsius: 1, MaxCelsius: 5, HumidityControlled: true));

        var room = Assert.Single(warehouses.Saved.Single().Rooms);
        Assert.Equal(1, room.Environment.MaintainedTemperature.MinCelsius);
        Assert.Equal(5, room.Environment.MaintainedTemperature.MaxCelsius);
        Assert.True(room.Environment.HumidityControlled);

        var published = outbox.PublishedMessage<RoomEnvironmentChangedV1>();
        Assert.Equal("WAW1", published.Warehouse);
        Assert.Equal("CHLD1", published.Room);
        Assert.Equal(1, published.MinCelsius);
        Assert.Equal(5, published.MaxCelsius);
    }

    [Fact]
    public async Task ChangeRoomEnvironment_for_a_missing_room_is_a_not_found()
    {
        var warehouses = new FakeWarehouseRepository();
        warehouses.Seed(Build.Warehouse("WAW1"));
        var handler = new ChangeRoomEnvironmentHandler(warehouses, TopologyOutbox.Create());

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => handler.HandleAsync(new ChangeRoomEnvironmentCommand("WAW1", "GHOST", 1, 5, false)));
    }
}
