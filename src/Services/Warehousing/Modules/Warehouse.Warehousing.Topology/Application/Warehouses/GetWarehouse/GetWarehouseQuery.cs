using Microsoft.EntityFrameworkCore;
using Warehouse.Warehousing.Topology.Domain;
using Warehouse.Warehousing.Topology.Infrastructure.Persistence;

namespace Warehouse.Warehousing.Topology.Application.Warehouses.GetWarehouse;

/// <summary>Read model for a single warehouse: its address, rooms (with environment + locations) and docks.</summary>
public sealed record GetWarehouseQuery(string Code);

public sealed record WarehouseDto(
    string Code,
    string Name,
    AddressDto Address,
    IReadOnlyList<RoomDto> Rooms,
    IReadOnlyList<DockDto> Docks);

public sealed record AddressDto(string Street, string City, string PostalCode, string CountryCode);

public sealed record RoomDto(
    string Code,
    string Type,
    RoomEnvironmentDto Environment,
    IReadOnlyList<LocationDto> Locations);

public sealed record RoomEnvironmentDto(decimal MinCelsius, decimal MaxCelsius, bool HumidityControlled);

public sealed record LocationDto(string Code, string Kind, decimal CapacityM3, decimal MaxLoadKg);

public sealed record DockDto(string Code, string Direction);

public sealed class GetWarehouseHandler(TopologyDbContext db)
{
    public async Task<WarehouseDto?> HandleAsync(GetWarehouseQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        var code = WarehouseCode.Of(query.Code);

        var site = await db.Warehouses
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == code, cancellationToken);

        return site is null ? null : Map(site);
    }

    internal static WarehouseDto Map(WarehouseSite w) => new(
        w.Code.Value,
        w.Name,
        new AddressDto(w.Address.Street, w.Address.City, w.Address.PostalCode, w.Address.CountryCode),
        w.Rooms
            .Select(r => new RoomDto(
                r.Code.Value,
                r.Type.ToString(),
                new RoomEnvironmentDto(
                    r.Environment.MaintainedTemperature.MinCelsius,
                    r.Environment.MaintainedTemperature.MaxCelsius,
                    r.Environment.HumidityControlled),
                r.Locations
                    .Select(l => new LocationDto(
                        l.Code.Value, l.Kind.ToString(), l.Capacity.CubicMeters, l.MaxLoad.Kilograms))
                    .ToList()))
            .ToList(),
        w.Docks
            .Select(d => new DockDto(d.Code.Value, d.Direction.ToString()))
            .ToList());
}
