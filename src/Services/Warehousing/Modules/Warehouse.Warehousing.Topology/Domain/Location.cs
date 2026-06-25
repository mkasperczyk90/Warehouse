using Warehouse.SharedKernel.Domain;
using Warehouse.SharedKernel.ValueObjects;

namespace Warehouse.Warehousing.Topology.Domain;

/// <summary>
/// Place archetype (🟩): the smallest addressable storage spot (rack/shelf/bin).
/// Capacity and load limit feed the Inventory put-away checks via LocationSnapshot.
/// </summary>
public sealed class Location : Entity<LocationCode>
{
    internal Location(LocationCode code, LocationKind kind, Volume capacity, Weight maxLoad)
        : base(code)
    {
        if (capacity == Volume.Zero)
        {
            throw new DomainException("location_capacity_required", $"Location {code} must have a positive capacity.");
        }

        Kind = kind;
        Capacity = capacity;
        MaxLoad = maxLoad;
    }

    private Location()
    {
    }

    public LocationCode Code => Id;

    public LocationKind Kind { get; private set; }

    public Volume Capacity { get; private set; }

    public Weight MaxLoad { get; private set; }

    internal void ChangeCapacity(Volume capacity, Weight maxLoad)
    {
        if (capacity == Volume.Zero)
        {
            throw new DomainException("location_capacity_required", $"Location {Code} must have a positive capacity.");
        }

        Capacity = capacity;
        MaxLoad = maxLoad;
    }
}
