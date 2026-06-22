using Warehouse.SharedKernel.ValueObjects;

namespace Warehouse.Warehousing.Inventory.Domain.Replicas;

/// <summary>
/// Local replica of a Topology location: environment + capacity, kept fresh by
/// LocationDefined/RoomEnvironmentChanged events from the Topology module.
/// </summary>
public sealed class LocationSnapshot
{
    public LocationSnapshot(
        LocationCode code,
        TemperatureRange environmentTemperature,
        bool isHazmatZone,
        Volume capacity,
        Weight maxLoad,
        DateTimeOffset updatedAt)
    {
        ArgumentNullException.ThrowIfNull(code);
        ArgumentNullException.ThrowIfNull(environmentTemperature);
        Code = code;
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

    public TemperatureRange EnvironmentTemperature { get; private set; } = null!;

    public bool IsHazmatZone { get; private set; }

    public Volume Capacity { get; private set; }

    public Weight MaxLoad { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }
}
