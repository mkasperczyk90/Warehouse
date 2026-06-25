using System.Globalization;
using Warehouse.Warehousing.Topology.Domain;

namespace Warehouse.Warehousing.Topology.Application.Tree;

/// <summary>
/// Presentation helpers shared by the admin topology read model (<see cref="GetTopologyTree"/> +
/// <see cref="GetRoom"/>). The desk renders a flat tree with icon keys and friendly room labels, so the
/// mapping from the domain's <see cref="RoomType"/>/environment lives here in one place.
/// </summary>
internal static class TopologyView
{
    /// <summary>Icon key the FE maps to a lucide icon; also the FE's <c>RoomType</c> union value.</summary>
    public static string Icon(RoomType type) => type switch
    {
        RoomType.ColdRoom => "cold",
        RoomType.Freezer => "freezer",
        RoomType.HazmatZone => "hazmat",
        _ => "standard",
    };

    public static string RoomLabel(RoomType type, string code) => type switch
    {
        RoomType.ColdRoom => $"Cold room {code}",
        RoomType.Freezer => $"Freezer {code}",
        RoomType.HazmatZone => $"Hazmat zone {code}",
        _ => $"Standard hall {code}",
    };

    /// <summary>Environment tag for a room node, e.g. "2–6 °C" (or "−18 °C" when min == max).</summary>
    public static string TempTag(RoomEnvironment environment)
    {
        var t = environment.MaintainedTemperature;
        return t.MinCelsius == t.MaxCelsius
            ? $"{Fmt(t.MinCelsius)} °C"
            : $"{Fmt(t.MinCelsius)}–{Fmt(t.MaxCelsius)} °C";
    }

    /// <summary>Split a room node id ("{warehouseCode}:{roomCode}") back into its parts; warehouse is
    /// <c>null</c> when the id is not a room node.</summary>
    public static (string? Warehouse, string Room) SplitRoomNodeId(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return (null, string.Empty);
        }

        var i = id.IndexOf(':');
        return i <= 0 || i == id.Length - 1 ? (null, string.Empty) : (id[..i], id[(i + 1)..]);
    }

    public static string RoomNodeId(string warehouseCode, string roomCode) => $"{warehouseCode}:{roomCode}";

    private static string Fmt(decimal c) => c.ToString("0.#", CultureInfo.InvariantCulture);
}
