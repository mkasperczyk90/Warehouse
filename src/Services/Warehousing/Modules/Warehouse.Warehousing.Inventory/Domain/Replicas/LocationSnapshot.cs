using Warehouse.SharedKernel.ValueObjects;

namespace Warehouse.Warehousing.Inventory.Domain.Replicas;

/// <summary>
/// Local replica of a Topology location: environment + capacity, kept fresh by
/// LocationDefined/RoomEnvironmentChanged events from the Topology module. <see cref="Warehouse"/> and
/// <see cref="Room"/> are carried so a room-environment change can refresh every location in the room
/// without a cross-service query (ADR-0003). Room codes repeat across warehouses, so a room is the pair.
/// </summary>
public sealed class LocationSnapshot
{
    public LocationSnapshot(
        LocationCode code,
        WarehouseCode warehouse,
        string room,
        TemperatureRange environmentTemperature,
        bool isHazmatZone,
        Volume capacity,
        Weight maxLoad,
        DateTimeOffset updatedAt)
    {
        ArgumentNullException.ThrowIfNull(code);
        ArgumentNullException.ThrowIfNull(warehouse);
        ArgumentException.ThrowIfNullOrWhiteSpace(room);
        ArgumentNullException.ThrowIfNull(environmentTemperature);
        Code = code;
        Warehouse = warehouse;
        Room = room;
        EnvironmentTemperature = environmentTemperature;
        IsHazmatZone = isHazmatZone;
        Capacity = capacity;
        MaxLoad = maxLoad;
        UpdatedAt = updatedAt;
    }

    private LocationSnapshot()
    {
    }

    public LocationCode Code { get; private set; } = null!;

    public WarehouseCode Warehouse { get; private set; } = null!;

    public string Room { get; private set; } = null!;

    public TemperatureRange EnvironmentTemperature { get; private set; } = null!;

    public bool IsHazmatZone { get; private set; }

    public Volume Capacity { get; private set; }

    public Weight MaxLoad { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>Re-projects the location from a later <c>LocationDefined</c> (capacity/environment may have
    /// been corrected). Identity (code, warehouse, room) is stable and not touched.</summary>
    public void Apply(
        TemperatureRange environmentTemperature,
        bool isHazmatZone,
        Volume capacity,
        Weight maxLoad,
        DateTimeOffset updatedAt)
    {
        ArgumentNullException.ThrowIfNull(environmentTemperature);
        EnvironmentTemperature = environmentTemperature;
        IsHazmatZone = isHazmatZone;
        Capacity = capacity;
        MaxLoad = maxLoad;
        UpdatedAt = updatedAt;
    }

    /// <summary>Refreshes only the maintained environment (a <c>RoomEnvironmentChanged</c> projection);
    /// capacity and load limit are properties of the location, not the room, so they stay.</summary>
    public void ApplyEnvironment(TemperatureRange environmentTemperature, DateTimeOffset updatedAt)
    {
        ArgumentNullException.ThrowIfNull(environmentTemperature);
        EnvironmentTemperature = environmentTemperature;
        UpdatedAt = updatedAt;
    }
}
