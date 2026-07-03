using Wolverine;
using Warehouse.Contracts.Logistics;
using Warehouse.ServiceDefaults.BiExport;

namespace Warehouse.Warehousing.Inventory.Application.BiExport;

/// <summary>
/// Exports a copy of every <see cref="PutAwayCompletedV1"/> to the Bronze landing zone
/// (docs/BI_Plan.md §5/§11 — feeds put-away lead-time metrics, §8). Same shape as
/// <see cref="GoodsReceivedBiExportConsumer"/>: a pure side observer on the shared
/// <c>warehousing.bi-export</c> queue, no domain writes.
/// </summary>
public sealed class PutAwayCompletedBiExportConsumer(IBronzeExportWriter writer)
{
    private const string SourceSystem = "warehouse-service";

    public Task Handle(PutAwayCompletedV1 message, Envelope envelope, CancellationToken cancellationToken)
    {
        var bronzeEvent = new BronzeEvent(
            SourceSystem,
            EventType: nameof(PutAwayCompletedV1),
            EventId: envelope.Id.ToString(), // Wolverine's own message id — stable across redeliveries.
            message.OccurredAt,
            Payload: new Dictionary<string, string?>
            {
                ["delivery_id"] = message.DeliveryId.ToString(),
            });

        return writer.WriteAsync(bronzeEvent, cancellationToken);
    }
}
