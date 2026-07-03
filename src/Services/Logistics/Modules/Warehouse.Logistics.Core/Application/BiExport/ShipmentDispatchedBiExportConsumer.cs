using Warehouse.Contracts.Logistics;
using Warehouse.ServiceDefaults.BiExport;
using Wolverine;

namespace Warehouse.Logistics.Core.Application.BiExport;

/// <summary>
/// Exports a copy of every <see cref="ShipmentDispatchedV1"/> to the Bronze landing zone
/// (docs/BI_Plan.md §5/§11 — feeds dispatch/on-time metrics, §8). Same shape as Warehousing's
/// <c>GoodsReceivedBiExportConsumer</c>: a pure side observer on its own dedicated queue, no domain
/// writes, no outbox.
/// </summary>
public sealed class ShipmentDispatchedBiExportConsumer(IBronzeExportWriter writer)
{
    private const string SourceSystem = "logistics-service";

    public Task Handle(ShipmentDispatchedV1 message, Envelope envelope, CancellationToken cancellationToken)
    {
        var bronzeEvent = new BronzeEvent(
            SourceSystem,
            EventType: nameof(ShipmentDispatchedV1),
            EventId: envelope.Id.ToString(), // Wolverine's own message id — stable across redeliveries.
            message.OccurredAt,
            Payload: new Dictionary<string, string?>
            {
                ["order_id"] = message.OrderId.ToString(),
                ["shipment_id"] = message.ShipmentId.ToString(),
                ["warehouse_code"] = message.WarehouseCode,
                ["carrier_role_id"] = message.CarrierRoleId,
                ["tracking_number"] = message.TrackingNumber,
            });

        return writer.WriteAsync(bronzeEvent, cancellationToken);
    }
}
