using Warehouse.Contracts.Topology;
using Warehouse.SharedKernel.ValueObjects;
using Warehouse.Warehousing.Topology.Application.Abstractions;
using Warehouse.Warehousing.Topology.Domain;
using Warehouse.Warehousing.Topology.Infrastructure.Persistence;
using Wolverine.EntityFrameworkCore;

namespace Warehouse.Warehousing.Topology.Application.Warehouses.ChangeLocationCapacity;

/// <summary>
/// Re-rate a storage location's capacity (m³) and load limit (kg) (UC-14). These feed the Inventory
/// put-away checks, so the new values are announced on the same <see cref="LocationDefinedV1"/> event a
/// fresh location uses — the consumer upserts the <c>LocationSnapshot</c>, so it is replay-safe.
/// </summary>
public sealed record ChangeLocationCapacityCommand(
    string Warehouse,
    string Room,
    string Location,
    decimal CapacityM3,
    decimal MaxLoadKg);

public sealed class ChangeLocationCapacityHandler(
    IWarehouseRepository warehouses, IDbContextOutbox<TopologyDbContext> outbox)
{
    public async Task HandleAsync(ChangeLocationCapacityCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var warehouseCode = WarehouseCode.Of(command.Warehouse);
        var site = await warehouses.GetByIdAsync(warehouseCode, cancellationToken)
            ?? throw new KeyNotFoundException($"Warehouse {warehouseCode} was not found.");

        var roomCode = RoomCode.Of(command.Room);
        var location = site.ChangeLocationCapacity(
            roomCode,
            LocationCode.Of(command.Location),
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
    }
}
