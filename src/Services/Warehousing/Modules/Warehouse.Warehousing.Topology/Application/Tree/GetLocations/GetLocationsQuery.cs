using Microsoft.EntityFrameworkCore;
using Warehouse.Warehousing.Topology.Domain;
using Warehouse.Warehousing.Topology.Infrastructure.Persistence;

namespace Warehouse.Warehousing.Topology.Application.Tree.GetLocations;

/// <summary>UC (global search): a flat list of every storage location across all warehouses, so the
/// gateway's search BFF can match "where is X" by address without walking the tree room by room.</summary>
public sealed record GetLocationsQuery;

public sealed record LocationSearchDto(string Code, string Room, string Warehouse);

public sealed class GetLocationsHandler(TopologyDbContext db)
{
    public async Task<IReadOnlyList<LocationSearchDto>> HandleAsync(CancellationToken cancellationToken = default)
    {
        var sites = await db.Warehouses.AsNoTracking().OrderBy(w => w.Id).ToListAsync(cancellationToken);
        return MapLocations(sites);
    }

    public static IReadOnlyList<LocationSearchDto> MapLocations(IEnumerable<WarehouseSite> sites) =>
        sites
            .SelectMany(w => w.Rooms.SelectMany(r => r.Locations.Select(l => new LocationSearchDto(
                l.Code.Value, TopologyView.RoomLabel(r.Type, r.Code.Value), w.Code.Value))))
            .ToList();
}
