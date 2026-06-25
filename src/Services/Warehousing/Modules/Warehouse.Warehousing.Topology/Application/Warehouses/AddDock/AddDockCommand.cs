using Warehouse.SharedKernel.Application;
using Warehouse.Warehousing.Topology.Application.Abstractions;
using Warehouse.Warehousing.Topology.Domain;

namespace Warehouse.Warehousing.Topology.Application.Warehouses.AddDock;

/// <summary>
/// Add a dock (ramp) to a warehouse (UC-14). <see cref="Direction"/> is one of <c>Inbound</c> /
/// <c>Outbound</c> / <c>Both</c>.
/// </summary>
public sealed record AddDockCommand(string Warehouse, string Code, string Direction);

public sealed class AddDockHandler(IWarehouseRepository warehouses, IUnitOfWork unitOfWork)
{
    public async Task<string> HandleAsync(AddDockCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var warehouseCode = WarehouseCode.Of(command.Warehouse);
        var site = await warehouses.GetByIdAsync(warehouseCode, cancellationToken)
            ?? throw new KeyNotFoundException($"Warehouse {warehouseCode} was not found.");

        var dock = site.AddDock(DockCode.Of(command.Code), RoomEnvironmentMap.ToDockDirection(command.Direction));

        warehouses.Update(site);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return dock.Code.Value;
    }
}
