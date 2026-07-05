using Warehouse.Contracts.Logistics;
using Warehouse.ServiceDefaults.BiExport;
using Wolverine;

namespace Warehouse.Logistics.Core.Application.BiExport;

/// <summary>
/// Exports a copy of every <see cref="DeliveryAnnouncedV1"/> to the Bronze landing zone
/// (docs/BI_Plan.md §5.3/§11 — feeds <c>BronzeInboundDelivery.announced_at</c>). Same shape as
/// <c>GoodsReceiptConfirmedBiExportConsumer</c>: a pure side observer on its own dedicated queue, no
/// domain writes, no outbox.
/// </summary>
public sealed class DeliveryAnnouncedBiExportConsumer(IBronzeExportWriter writer)
{
    private const string SourceSystem = "logistics-service";

    public Task Handle(DeliveryAnnouncedV1 message, Envelope envelope, CancellationToken cancellationToken)
    {
        var bronzeEvent = new BronzeEvent(
            SourceSystem,
            EventType: nameof(DeliveryAnnouncedV1),
            EventId: envelope.Id.ToString(),
            message.OccurredAt,
            Payload: new Dictionary<string, string?>
            {
                ["delivery_id"] = message.DeliveryId.ToString(),
                ["warehouse_code"] = message.WarehouseCode,
                ["supplier_party_id"] = message.SupplierRoleId,
                ["planned_at"] = message.PlannedAt.ToString("O"),
            });

        return writer.WriteAsync(bronzeEvent, cancellationToken);
    }
}
