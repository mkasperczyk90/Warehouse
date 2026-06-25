using Microsoft.EntityFrameworkCore;
using Warehouse.Warehousing.Topology.Domain;
using Warehouse.Warehousing.Topology.Infrastructure.Persistence;

namespace Warehouse.Warehousing.Topology.Application.Tree.GetRoom;

/// <summary>UC-14 — the detail behind a room node selected in the topology tree: the room's environment
/// and its storage locations. <see cref="NodeId"/> is the tree node id ("{warehouseCode}:{roomCode}").</summary>
public sealed record GetRoomQuery(string NodeId);

public sealed record RoomDetailDto(
    string Id,
    string Name,
    string Warehouse,
    string Type,
    decimal TempMin,
    decimal TempMax,
    int ShownCount,
    int TotalCount,
    IReadOnlyList<LocationRowDto> Locations);

/// <summary><see cref="Occupied"/> is a presentation string; occupancy is Inventory's stock, not a
/// Topology concern, so it reads "—" here until a stock-occupancy projection is composed in.</summary>
public sealed record LocationRowDto(string Id, string Address, decimal Capacity, decimal LoadLimit, string Occupied);

public sealed class GetRoomHandler(TopologyDbContext db)
{
    public async Task<RoomDetailDto?> HandleAsync(GetRoomQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var (warehouseCode, roomCode) = TopologyView.SplitRoomNodeId(query.NodeId);
        if (warehouseCode is null)
        {
            return null;
        }

        var code = WarehouseCode.Of(warehouseCode);
        var site = await db.Warehouses.AsNoTracking().FirstOrDefaultAsync(w => w.Id == code, cancellationToken);
        var room = site?.Rooms.FirstOrDefault(r => r.Code.Value == roomCode);
        return site is null || room is null ? null : MapRoom(site, room);
    }

    public static RoomDetailDto MapRoom(WarehouseSite site, Room room)
    {
        var locations = room.Locations
            .Select(l => new LocationRowDto(
                l.Code.Value, l.Code.Value, l.Capacity.CubicMeters, l.MaxLoad.Kilograms, "—"))
            .ToList();

        var temperature = room.Environment.MaintainedTemperature;
        return new RoomDetailDto(
            TopologyView.RoomNodeId(site.Code.Value, room.Code.Value),
            TopologyView.RoomLabel(room.Type, room.Code.Value),
            site.Code.Value,
            TopologyView.Icon(room.Type),
            temperature.MinCelsius,
            temperature.MaxCelsius,
            locations.Count,
            locations.Count,
            locations);
    }
}
