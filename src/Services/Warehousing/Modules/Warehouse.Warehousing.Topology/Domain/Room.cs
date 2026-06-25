using Warehouse.SharedKernel.Domain;
using Warehouse.SharedKernel.ValueObjects;

namespace Warehouse.Warehousing.Topology.Domain;

/// <summary>
/// Place archetype (🟩): a separated part of a warehouse with controlled conditions
/// (standard, cold room, freezer, hazmat zone). Owns its storage locations.
/// </summary>
public sealed class Room : Entity<RoomCode>
{
    private readonly List<Location> _locations = [];

    internal Room(RoomCode code, RoomType type, RoomEnvironment environment) : base(code)
    {
        Type = type;
        Environment = environment;
    }

    private Room()
    {
    }

    public RoomCode Code => Id;

    public RoomType Type { get; private set; }

    public RoomEnvironment Environment { get; private set; }  = null!;

    public IReadOnlyCollection<Location> Locations => _locations.AsReadOnly();

    internal Location AddLocation(LocationCode code, LocationKind kind, Volume capacity, Weight maxLoad)
    {
        if (_locations.Any(l => l.Code == code))
        {
            throw new DomainException("location_code_duplicate", $"Location {code} already exists in room {Code}.");
        }

        var location = new Location(code, kind, capacity, maxLoad);
        _locations.Add(location);
        return location;
    }

    internal void ChangeEnvironment(RoomEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(environment);
        Environment = environment;
    }

    internal Location ChangeLocationCapacity(LocationCode code, Volume capacity, Weight maxLoad)
    {
        var location = _locations.SingleOrDefault(l => l.Code == code)
            ?? throw new DomainException("location_not_found", $"Location {code} does not exist in room {Code}.");
        location.ChangeCapacity(capacity, maxLoad);
        return location;
    }
}
