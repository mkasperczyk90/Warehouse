using Microsoft.EntityFrameworkCore;
using Warehouse.Warehousing.Topology.Infrastructure.Persistence;

namespace Warehouse.Warehousing.Topology.Application.Warehouses.ListWarehouses;

/// <summary>List warehouse sites with structural counts (rooms / docks / locations).</summary>
public sealed record ListWarehousesQuery;

public sealed record WarehouseSummaryDto(
    string Code,
    string Name,
    string City,
    string CountryCode,
    int RoomCount,
    int DockCount,
    int LocationCount);

public sealed class ListWarehousesHandler(TopologyDbContext db)
{
    public async Task<IReadOnlyList<WarehouseSummaryDto>> HandleAsync(
        ListWarehousesQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        // The warehouse is the aggregate's only entry point: its rooms, locations and docks are owned and
        // load with it. The site list is small, so we materialize the aggregates and count in memory.
        var sites = await db.Warehouses
            .AsNoTracking()
            .OrderBy(w => w.Id)
            .ToListAsync(cancellationToken);

        return sites
            .Select(w => new WarehouseSummaryDto(
                w.Code.Value,
                w.Name,
                w.Address.City,
                w.Address.CountryCode,
                w.Rooms.Count,
                w.Docks.Count,
                w.Rooms.Sum(r => r.Locations.Count)))
            .ToList();
    }
}
