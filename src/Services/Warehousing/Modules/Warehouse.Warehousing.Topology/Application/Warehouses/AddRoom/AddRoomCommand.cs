using Warehouse.SharedKernel.Application;
using Warehouse.Warehousing.Topology.Application.Abstractions;
using Warehouse.Warehousing.Topology.Domain;

namespace Warehouse.Warehousing.Topology.Application.Warehouses.AddRoom;

/// <summary>
/// Add a room to a warehouse (UC-14). <see cref="Type"/> is one of <c>Standard</c> / <c>ColdRoom</c> /
/// <c>Freezer</c> / <c>HazmatZone</c>; <see cref="MinCelsius"/>/<see cref="MaxCelsius"/> carry the
/// maintained range when it differs from the type's default.
/// </summary>
public sealed record AddRoomCommand(
    string Warehouse,
    string Code,
    string Type,
    decimal? MinCelsius,
    decimal? MaxCelsius,
    bool HumidityControlled);

public sealed class AddRoomHandler(IWarehouseRepository warehouses, IUnitOfWork unitOfWork)
{
    public async Task<string> HandleAsync(AddRoomCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var warehouseCode = WarehouseCode.Of(command.Warehouse);
        var site = await warehouses.GetByIdAsync(warehouseCode, cancellationToken)
            ?? throw new KeyNotFoundException($"Warehouse {warehouseCode} was not found.");

        var type = RoomEnvironmentMap.ToRoomType(command.Type);
        var environment = RoomEnvironmentMap.ToEnvironment(
            type, command.MinCelsius, command.MaxCelsius, command.HumidityControlled);

        var room = site.AddRoom(RoomCode.Of(command.Code), type, environment);

        warehouses.Update(site);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return room.Code.Value;
    }
}
