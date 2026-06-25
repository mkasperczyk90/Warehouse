using Warehouse.Contracts.Topology;
using Warehouse.SharedKernel.ValueObjects;
using Warehouse.Warehousing.Topology.Application.Abstractions;
using Warehouse.Warehousing.Topology.Domain;
using Warehouse.Warehousing.Topology.Infrastructure.Persistence;
using Wolverine.EntityFrameworkCore;

namespace Warehouse.Warehousing.Topology.Application.Warehouses.AddLocation;

/// <summary>
/// Add a storage location to a room (UC-14). <see cref="Kind"/> is one of <c>Rack</c> / <c>Floor</c> /
/// <c>DockBuffer</c>; capacity (m³) and load limit (kg) feed the Inventory put-away checks.
/// </summary>
public sealed record AddLocationCommand(
    string Warehouse,
    string Room,
    string Code,
    string Kind,
    decimal CapacityM3,
    decimal MaxLoadKg);

/// <summary>
/// Adds the location to the aggregate and announces it through the transactional outbox: the location
/// row and the <see cref="LocationDefinedV1"/> event commit as one transaction, so Inventory's
/// <c>LocationSnapshot</c> replica can never drift from a location that was actually created.
/// </summary>
public sealed class AddLocationHandler(IWarehouseRepository warehouses, IDbContextOutbox<TopologyDbContext> outbox)
{
    public async Task<string> HandleAsync(AddLocationCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var warehouseCode = WarehouseCode.Of(command.Warehouse);
        var site = await warehouses.GetByIdAsync(warehouseCode, cancellationToken)
            ?? throw new KeyNotFoundException($"Warehouse {warehouseCode} was not found.");

        var roomCode = RoomCode.Of(command.Room);
        var location = site.AddLocation(
            roomCode,
            LocationCode.Of(command.Code),
            RoomEnvironmentMap.ToLocationKind(command.Kind),
            Volume.FromCubicMeters(command.CapacityM3),
            Weight.FromKilograms(command.MaxLoadKg));

        var room = site.Rooms.Single(r => r.Code == roomCode);
        var temperature = room.Environment.MaintainedTemperature;

        warehouses.Update(site);

        await outbox.PublishAsync(new LocationDefinedV1(
            site.Code.Value,
            room.Code.Value,
            location.Code.Value,
            location.Kind.ToString(),
            location.Capacity.CubicMeters,
            location.MaxLoad.Kilograms,
            temperature.MinCelsius,
            temperature.MaxCelsius,
            room.Type == RoomType.HazmatZone,
            DateTimeOffset.UtcNow));

        await outbox.SaveChangesAndFlushMessagesAsync(cancellationToken);
        return location.Code.Value;
    }
}
