using Warehouse.Contracts.Topology;
using Warehouse.Warehousing.Topology.Application.Abstractions;
using Warehouse.Warehousing.Topology.Domain;
using Warehouse.Warehousing.Topology.Infrastructure.Persistence;
using Wolverine.EntityFrameworkCore;

namespace Warehouse.Warehousing.Topology.Application.Warehouses.ChangeRoomEnvironment;

/// <summary>
/// Re-tune a room's maintained conditions (UC-14). The room type is fixed (a freezer stays a freezer); the
/// new range is validated against it by the domain. Changing the environment never moves goods — Inventory
/// re-checks compatibility on the <see cref="RoomEnvironmentChangedV1"/> event and reports.
/// </summary>
public sealed record ChangeRoomEnvironmentCommand(
    string Warehouse,
    string Room,
    decimal? MinCelsius,
    decimal? MaxCelsius,
    bool HumidityControlled);

/// <summary>
/// Applies the new environment to the aggregate and announces it through the transactional outbox: the
/// room change and the <see cref="RoomEnvironmentChangedV1"/> event commit as one transaction.
/// </summary>
public sealed class ChangeRoomEnvironmentHandler(
    IWarehouseRepository warehouses, IDbContextOutbox<TopologyDbContext> outbox)
{
    public async Task HandleAsync(ChangeRoomEnvironmentCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var warehouseCode = WarehouseCode.Of(command.Warehouse);
        var site = await warehouses.GetByIdAsync(warehouseCode, cancellationToken)
            ?? throw new KeyNotFoundException($"Warehouse {warehouseCode} was not found.");

        var roomCode = RoomCode.Of(command.Room);
        var room = site.Rooms.SingleOrDefault(r => r.Code == roomCode)
            ?? throw new KeyNotFoundException($"Room {roomCode} does not exist in warehouse {warehouseCode}.");

        var environment = RoomEnvironmentMap.ToEnvironment(
            room.Type, command.MinCelsius, command.MaxCelsius, command.HumidityControlled);

        site.ChangeRoomEnvironment(roomCode, environment);

        var temperature = environment.MaintainedTemperature;
        warehouses.Update(site);

        await outbox.PublishAsync(new RoomEnvironmentChangedV1(
            site.Code.Value,
            roomCode.Value,
            temperature.MinCelsius,
            temperature.MaxCelsius,
            DateTimeOffset.UtcNow));

        await outbox.SaveChangesAndFlushMessagesAsync(cancellationToken);
    }
}
