using Microsoft.EntityFrameworkCore;
using Warehouse.Warehousing.Topology.Domain;
using Warehouse.Warehousing.Topology.Infrastructure.Persistence;

namespace Warehouse.Warehousing.Topology.Application.Tree.GetTopologyTree;

/// <summary>UC-14 — the cross-warehouse topology tree the admin Topology screen renders: a flat, ordered
/// list of warehouse + room nodes (locations are loaded with the room detail, not the tree). Topology is
/// master data, so the tree is not scoped to the active warehouse.</summary>
public sealed record GetTopologyTreeQuery;

/// <summary>One tree node. <see cref="Level"/> is 1 (warehouse) or 2 (room); <see cref="Icon"/> is the
/// FE icon key; <see cref="Tag"/> carries a room's environment, e.g. "2–6 °C".</summary>
public sealed record TopologyNodeDto(string Id, int Level, string Label, string Kind, string Icon, string? Tag);

public sealed class GetTopologyTreeHandler(TopologyDbContext db)
{
    public async Task<IReadOnlyList<TopologyNodeDto>> HandleAsync(CancellationToken cancellationToken = default)
    {
        // The site list is small and the rooms load with the aggregate; materialize and project in memory.
        var sites = await db.Warehouses.AsNoTracking().OrderBy(w => w.Id).ToListAsync(cancellationToken);
        return MapTree(sites);
    }

    public static IReadOnlyList<TopologyNodeDto> MapTree(IEnumerable<WarehouseSite> sites)
    {
        var nodes = new List<TopologyNodeDto>();
        foreach (var w in sites)
        {
            nodes.Add(new TopologyNodeDto(w.Code.Value, 1, $"{w.Code.Value} {w.Name}", "warehouse", "warehouse", null));
            foreach (var r in w.Rooms)
            {
                nodes.Add(new TopologyNodeDto(
                    TopologyView.RoomNodeId(w.Code.Value, r.Code.Value),
                    2,
                    TopologyView.RoomLabel(r.Type, r.Code.Value),
                    "room",
                    TopologyView.Icon(r.Type),
                    TopologyView.TempTag(r.Environment)));
            }
        }

        return nodes;
    }
}
