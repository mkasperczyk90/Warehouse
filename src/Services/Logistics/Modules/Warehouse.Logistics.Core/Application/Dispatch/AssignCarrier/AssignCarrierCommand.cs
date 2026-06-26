using Warehouse.Logistics.Core.Application.Abstractions;
using Warehouse.Logistics.Core.Domain;
using Warehouse.SharedKernel.Application;

namespace Warehouse.Logistics.Core.Application.Dispatch.AssignCarrier;

/// <summary>UC-12 — book a carrier and pickup slot for a packed shipment on the dispatch board
/// (AwaitingCarrier → CarrierAssigned).</summary>
public sealed record AssignCarrierCommand(Guid ShipmentId, string CarrierRoleId, string Pickup);

public sealed class AssignCarrierHandler(IShipmentRepository shipments, IUnitOfWork unitOfWork)
{
    public async Task HandleAsync(AssignCarrierCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        var shipment = await shipments.GetByIdAsync(new ShipmentId(command.ShipmentId), cancellationToken)
            ?? throw new KeyNotFoundException($"Shipment {command.ShipmentId} not found.");

        shipment.AssignCarrier(new PartyRoleRef(command.CarrierRoleId), command.Pickup);
        shipments.Update(shipment);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
