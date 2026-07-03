using Warehouse.Contracts.Logistics;
using Warehouse.ServiceDefaults.BiExport;
using Wolverine;

namespace Warehouse.Logistics.Core.Application.BiExport;

/// <summary>
/// Exports a copy of every <see cref="PickingReleasedV1"/> to the Bronze landing zone
/// (docs/BI_Plan.md §5/§11 — feeds picking metrics, §8). Same shape as Warehousing's
/// <c>GoodsReceivedBiExportConsumer</c>: a pure side observer on its own dedicated queue, no domain
/// writes, no outbox.
/// </summary>
public sealed class PickingReleasedBiExportConsumer(IBronzeExportWriter writer)
{
    private const string SourceSystem = "logistics-service";

    public Task Handle(PickingReleasedV1 message, Envelope envelope, CancellationToken cancellationToken)
    {
        var bronzeEvent = new BronzeEvent(
            SourceSystem,
            EventType: nameof(PickingReleasedV1),
            EventId: envelope.Id.ToString(), // Wolverine's own message id — stable across redeliveries.
            message.OccurredAt,
            Payload: new Dictionary<string, string?>
            {
                ["order_id"] = message.OrderId.ToString(),
                ["warehouse_code"] = message.WarehouseCode,
            });

        return writer.WriteAsync(bronzeEvent, cancellationToken);
    }
}
