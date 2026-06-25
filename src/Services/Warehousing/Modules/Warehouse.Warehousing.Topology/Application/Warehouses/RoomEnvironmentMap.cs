using Warehouse.SharedKernel.Domain;
using Warehouse.SharedKernel.ValueObjects;
using Warehouse.Warehousing.Topology.Domain;

namespace Warehouse.Warehousing.Topology.Application.Warehouses;

/// <summary>
/// Translates the primitive shape the API speaks (a room-type name + optional temperature bounds) to and
/// from the domain's <see cref="RoomType"/> / <see cref="RoomEnvironment"/>. Shared by the add-room and
/// change-environment slices so the wire contract stays in one place.
/// </summary>
internal static class RoomEnvironmentMap
{
    public static RoomType ToRoomType(string type)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        return Enum.TryParse<RoomType>(type, ignoreCase: true, out var roomType)
            ? roomType
            : throw new DomainException(
                "room_type_unknown",
                $"Unknown room type '{type}'. Use Standard, ColdRoom, Freezer or HazmatZone.");
    }

    public static LocationKind ToLocationKind(string kind)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(kind);
        return Enum.TryParse<LocationKind>(kind, ignoreCase: true, out var locationKind)
            ? locationKind
            : throw new DomainException(
                "location_kind_unknown",
                $"Unknown location kind '{kind}'. Use Rack, Floor or DockBuffer.");
    }

    public static DockDirection ToDockDirection(string direction)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(direction);
        return Enum.TryParse<DockDirection>(direction, ignoreCase: true, out var dockDirection)
            ? dockDirection
            : throw new DomainException(
                "dock_direction_unknown",
                $"Unknown dock direction '{direction}'. Use Inbound, Outbound or Both.");
    }

    /// <summary>
    /// Builds the room environment for a room of <paramref name="type"/>. When both bounds are given the
    /// range is explicit; otherwise the type's default range applies. The aggregate's value object
    /// re-checks the range against the room type, so an impossible setting (e.g. a "freezer" at +4°C) is
    /// rejected by the domain.
    /// </summary>
    public static RoomEnvironment ToEnvironment(
        RoomType type, decimal? minCelsius, decimal? maxCelsius, bool humidityControlled)
    {
        TemperatureRange? range = minCelsius is { } min && maxCelsius is { } max
            ? TemperatureRange.Of(min, max)
            : null;

        return RoomEnvironment.For(type, range, humidityControlled);
    }
}
