using Warehouse.SharedKernel.Domain;
using Warehouse.SharedKernel.ValueObjects;

namespace Warehouse.Warehousing.Topology.Domain;

/// <summary>
/// The conditions a room maintains. The counterpart of the product's StorageRequirement
/// in the compatibility rule; consistency with <see cref="RoomType"/> is enforced here.
/// </summary>
public sealed record RoomEnvironment
{
    private RoomEnvironment(TemperatureRange maintainedTemperature, bool humidityControlled)
    {
        MaintainedTemperature = maintainedTemperature;
        HumidityControlled = humidityControlled;
    }

    public TemperatureRange MaintainedTemperature { get; }

    public bool HumidityControlled { get; }

    public static RoomEnvironment For(RoomType type, TemperatureRange? maintainedTemperature = null, bool humidityControlled = false)
    {
        var temperature = maintainedTemperature ?? DefaultTemperature(type);
        Validate(type, temperature);
        return new RoomEnvironment(temperature, humidityControlled);
    }

    private static TemperatureRange DefaultTemperature(RoomType type) => type switch
    {
        RoomType.ColdRoom => TemperatureRange.Of(0, 8),
        RoomType.Freezer => TemperatureRange.Of(-25, -18),
        _ => TemperatureRange.Ambient,
    };

    private static void Validate(RoomType type, TemperatureRange temperature)
    {
        switch (type)
        {
            case RoomType.ColdRoom when temperature.MaxCelsius > 8:
                throw new DomainException(
                    "room_environment_invalid",
                    $"A cold room cannot maintain more than 8°C, got {temperature}.");

            case RoomType.Freezer when temperature.MaxCelsius > -15:
                throw new DomainException(
                    "room_environment_invalid",
                    $"A freezer must maintain -15°C or below, got {temperature}.");
        }
    }
}
