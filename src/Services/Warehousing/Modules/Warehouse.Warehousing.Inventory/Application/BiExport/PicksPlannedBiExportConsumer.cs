using System.Globalization;
using System.Text.Json;
using Wolverine;
using Warehouse.Contracts.Logistics;
using Warehouse.ServiceDefaults.BiExport;

namespace Warehouse.Warehousing.Inventory.Application.BiExport;

/// <summary>
/// Exports a copy of every <see cref="PicksPlannedV1"/> to the Bronze landing zone (docs/BI_Plan.md
/// §5/§11 — feeds picking metrics, §8). Same shape as <see cref="GoodsReceivedBiExportConsumer"/>: a
/// pure side observer on the shared <c>warehousing.bi-export</c> queue, no domain writes.
/// <para>
/// <see cref="PicksPlannedV1.Picks"/> is a nested list, which doesn't fit the flat
/// <see cref="BronzeEvent.Payload"/> string map. Rather than special-case the writer for one event's
/// shape, the list is carried as a JSON string (<c>picks_json</c>) — Bronze is schema-on-read
/// tolerant by design (docs/BI_Plan.md §5); Silver is where it gets exploded into per-pick rows.
/// </para>
/// </summary>
public sealed class PicksPlannedBiExportConsumer(IBronzeExportWriter writer)
{
    private const string SourceSystem = "warehouse-service";

    public Task Handle(PicksPlannedV1 message, Envelope envelope, CancellationToken cancellationToken)
    {
        var bronzeEvent = new BronzeEvent(
            SourceSystem,
            EventType: nameof(PicksPlannedV1),
            EventId: envelope.Id.ToString(), // Wolverine's own message id — stable across redeliveries.
            message.OccurredAt,
            Payload: new Dictionary<string, string?>
            {
                ["order_id"] = message.OrderId.ToString(),
                ["pick_count"] = message.Picks.Count.ToString(CultureInfo.InvariantCulture),
                ["picks_json"] = JsonSerializer.Serialize(message.Picks),
            });

        return writer.WriteAsync(bronzeEvent, cancellationToken);
    }
}
