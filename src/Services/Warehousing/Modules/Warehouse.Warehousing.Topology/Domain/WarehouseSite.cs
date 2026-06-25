using Warehouse.SharedKernel.Domain;
using Warehouse.SharedKernel.ValueObjects;
using Warehouse.Warehousing.Topology.Domain.Events;

namespace Warehouse.Warehousing.Topology.Domain;

/// <summary>
/// Place archetype (🟩): a physical warehouse — "Warehouse" in the ubiquitous language
/// (named WarehouseSite in code only to avoid clashing with the root namespace).
/// The aggregate guards structural consistency: unique room/dock/location codes,
/// environment rules per room type. Deleting non-empty rooms is prevented by an
/// application-level policy that consults Inventory.
/// </summary>
public sealed class WarehouseSite : AggregateRoot<WarehouseCode>
{
    private readonly List<Room> _rooms = [];
    private readonly List<Dock> _docks = [];

    private WarehouseSite(WarehouseCode code, string name, Address address) : base(code)
    {
        Name = name;
        Address = address;
    }

    private WarehouseSite()
    {
    }

    public WarehouseCode Code => Id;

    public string Name { get; private set; } = null!;

    public Address Address { get; private set; } = null!;

    public IReadOnlyCollection<Room> Rooms => _rooms.AsReadOnly();

    public IReadOnlyCollection<Dock> Docks => _docks.AsReadOnly();

    public static WarehouseSite Establish(WarehouseCode code, string name, Address address)
    {
        ArgumentNullException.ThrowIfNull(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(address);
        return new WarehouseSite(code, name.Trim(), address);
    }

    public Room AddRoom(RoomCode code, RoomType type, RoomEnvironment? environment = null)
    {
        ArgumentNullException.ThrowIfNull(code);
        if (_rooms.Any(r => r.Code == code))
        {
            throw new DomainException("room_code_duplicate", $"Room {code} already exists in warehouse {Code}.");
        }

        var room = new Room(code, type, environment ?? RoomEnvironment.For(type));
        _rooms.Add(room);
        return room;
    }

    public Location AddLocation(RoomCode roomCode, LocationCode code, LocationKind kind, Volume capacity, Weight maxLoad)
    {
        ArgumentNullException.ThrowIfNull(roomCode);
        ArgumentNullException.ThrowIfNull(code);
        var room = GetRoom(roomCode);

        if (_rooms.SelectMany(r => r.Locations).Any(l => l.Code == code))
        {
            throw new DomainException("location_code_duplicate", $"Location {code} already exists in warehouse {Code}.");
        }

        var location = room.AddLocation(code, kind, capacity, maxLoad);
        Raise(new LocationDefined(
            Code, room.Code, location.Code, kind, capacity, maxLoad, room.Environment, DateTimeOffset.UtcNow));
        return location;
    }

    public Dock AddDock(DockCode code, DockDirection direction)
    {
        ArgumentNullException.ThrowIfNull(code);
        if (_docks.Any(d => d.Code == code))
        {
            throw new DomainException("dock_code_duplicate", $"Dock {code} already exists in warehouse {Code}.");
        }

        var dock = new Dock(code, direction);
        _docks.Add(dock);
        return dock;
    }

    public void ChangeRoomEnvironment(RoomCode roomCode, RoomEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(roomCode);
        ArgumentNullException.ThrowIfNull(environment);
        var room = GetRoom(roomCode);
        room.ChangeEnvironment(environment);
        Raise(new RoomEnvironmentChanged(Code, room.Code, environment, DateTimeOffset.UtcNow));
    }

    public Location ChangeLocationCapacity(RoomCode roomCode, LocationCode code, Volume capacity, Weight maxLoad)
    {
        ArgumentNullException.ThrowIfNull(roomCode);
        ArgumentNullException.ThrowIfNull(code);
        return GetRoom(roomCode).ChangeLocationCapacity(code, capacity, maxLoad);
    }

    private Room GetRoom(RoomCode roomCode) =>
        _rooms.SingleOrDefault(r => r.Code == roomCode)
        ?? throw new DomainException("room_not_found", $"Room {roomCode} does not exist in warehouse {Code}.");
}
