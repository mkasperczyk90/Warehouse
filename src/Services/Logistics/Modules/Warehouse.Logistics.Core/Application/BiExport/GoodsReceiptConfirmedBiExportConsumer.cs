using System.Globalization;
using System.Text.Json;
using Warehouse.Contracts.Logistics;
using Warehouse.ServiceDefaults.BiExport;
using Wolverine;

namespace Warehouse.Logistics.Core.Application.BiExport;

/// <summary>
/// Exports a copy of every <see cref="GoodsReceiptConfirmedV1"/> to the Bronze landing zone
/// (docs/BI_Plan.md §5/§11 — feeds inbound receipt metrics, §8). Same shape as Warehousing's
/// <c>GoodsReceivedBiExportConsumer</c>: a pure side observer on its own dedicated queue, no domain
/// writes, no outbox.
/// <para>
/// <see cref="GoodsReceiptConfirmedV1.Lines"/> is a nested list, which doesn't fit the flat
/// <see cref="BronzeEvent.Payload"/> string map — it travels as a JSON string (<c>lines_json</c>)
/// alongside a <c>line_count</c>, same pattern as Warehousing's <c>PicksPlannedBiExportConsumer</c>.
/// </para>
/// </summary>
public sealed class GoodsReceiptConfirmedBiExportConsumer(IBronzeExportWriter writer)
{
    private const string SourceSystem = "logistics-service";

    public Task Handle(GoodsReceiptConfirmedV1 message, Envelope envelope, CancellationToken cancellationToken)
    {
        var bronzeEvent = new BronzeEvent(
            SourceSystem,
            EventType: nameof(GoodsReceiptConfirmedV1),
            EventId: envelope.Id.ToString(), // Wolverine's own message id — stable across redeliveries.
            message.OccurredAt,
            Payload: new Dictionary<string, string?>
            {
                ["delivery_id"] = message.DeliveryId.ToString(),
                ["warehouse_code"] = message.WarehouseCode,
                ["line_count"] = message.Lines.Count.ToString(CultureInfo.InvariantCulture),
                ["lines_json"] = JsonSerializer.Serialize(message.Lines),
            });

        return writer.WriteAsync(bronzeEvent, cancellationToken);
    }
}
