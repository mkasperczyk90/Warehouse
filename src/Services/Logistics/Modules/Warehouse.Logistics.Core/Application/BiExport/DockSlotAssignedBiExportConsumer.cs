using Warehouse.Contracts.Logistics;
using Warehouse.ServiceDefaults.BiExport;
using Wolverine;

namespace Warehouse.Logistics.Core.Application.BiExport;

/// <summary>
/// Exports a copy of every <see cref="DockSlotAssignedV1"/> to the Bronze landing zone
/// (docs/BI_Plan.md §5.3/§11 — feeds <c>BronzeInboundDelivery.dock_code</c>/<c>dock_slot_at</c>).
/// Same shape as <c>GoodsReceiptConfirmedBiExportConsumer</c>: a pure side observer on its own
/// dedicated queue, no domain writes, no outbox.
/// </summary>
public sealed class DockSlotAssignedBiExportConsumer(IBronzeExportWriter writer)
{
    private const string SourceSystem = "logistics-service";

    public Task Handle(DockSlotAssignedV1 message, Envelope envelope, CancellationToken cancellationToken)
    {
        var bronzeEvent = new BronzeEvent(
            SourceSystem,
            EventType: nameof(DockSlotAssignedV1),
            EventId: envelope.Id.ToString(),
            message.OccurredAt,
            Payload: new Dictionary<string, string?>
            {
                ["delivery_id"] = message.DeliveryId.ToString(),
                ["warehouse_code"] = message.WarehouseCode,
                ["dock_code"] = message.DockCode,
                ["dock_slot_at"] = message.SlotFrom.ToString("O"),
                ["dock_slot_to"] = message.SlotTo.ToString("O"),
            });

        return writer.WriteAsync(bronzeEvent, cancellationToken);
    }
}
