using System.Text.RegularExpressions;
using Warehouse.SharedKernel.Domain;

namespace Warehouse.Warehousing.Topology.Domain;

/// <summary>Short code of a warehouse site, e.g. "WAW1".</summary>
public sealed partial record WarehouseCode
{
    private WarehouseCode(string value) => Value = value;

    public string Value { get; }

    public static WarehouseCode Of(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var normalized = value.Trim().ToUpperInvariant();
        return CodePattern().IsMatch(normalized)
            ? new WarehouseCode(normalized)
            : throw new DomainException("warehouse_code_invalid", $"'{value}' is not a valid warehouse code (2-10 chars A-Z, 0-9).");
    }

    public override string ToString() => Value;

    [GeneratedRegex("^[A-Z0-9]{2,10}$")]
    private static partial Regex CodePattern();
}

/// <summary>Code of a room within a warehouse, e.g. "CHLD1".</summary>
public sealed partial record RoomCode
{
    private RoomCode(string value) => Value = value;

    public string Value { get; }

    public static RoomCode Of(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var normalized = value.Trim().ToUpperInvariant();
        return CodePattern().IsMatch(normalized)
            ? new RoomCode(normalized)
            : throw new DomainException("room_code_invalid", $"'{value}' is not a valid room code (2-10 chars A-Z, 0-9).");
    }

    public override string ToString() => Value;

    [GeneratedRegex("^[A-Z0-9]{2,10}$")]
    private static partial Regex CodePattern();
}

/// <summary>Code of a dock (ramp), e.g. "D01".</summary>
public sealed partial record DockCode
{
    private DockCode(string value) => Value = value;

    public string Value { get; }

    public static DockCode Of(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var normalized = value.Trim().ToUpperInvariant();
        return CodePattern().IsMatch(normalized)
            ? new DockCode(normalized)
            : throw new DomainException("dock_code_invalid", $"'{value}' is not a valid dock code (2-10 chars A-Z, 0-9).");
    }

    public override string ToString() => Value;

    [GeneratedRegex("^[A-Z0-9]{2,10}$")]
    private static partial Regex CodePattern();
}

/// <summary>
/// Stable, scannable address of a storage location: WH-ROOM-AISLE-RACK-SHELF
/// (2 to 5 dash-separated segments). Printed as a barcode on the physical rack.
/// </summary>
public sealed partial record LocationCode
{
    private LocationCode(string value) => Value = value;

    public string Value { get; }

    public static LocationCode Of(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var normalized = value.Trim().ToUpperInvariant();
        return CodePattern().IsMatch(normalized)
            ? new LocationCode(normalized)
            : throw new DomainException(
                "location_code_invalid",
                $"'{value}' is not a valid location code (segments of A-Z/0-9 joined with '-').");
    }

    public static LocationCode Compose(WarehouseCode warehouse, RoomCode room, params string[] segments)
    {
        ArgumentNullException.ThrowIfNull(warehouse);
        ArgumentNullException.ThrowIfNull(room);
        var all = new[] { warehouse.Value, room.Value }.Concat(segments);
        return Of(string.Join('-', all));
    }

    public override string ToString() => Value;

    [GeneratedRegex("^[A-Z0-9]{1,10}(-[A-Z0-9]{1,10}){1,4}$")]
    private static partial Regex CodePattern();
}
