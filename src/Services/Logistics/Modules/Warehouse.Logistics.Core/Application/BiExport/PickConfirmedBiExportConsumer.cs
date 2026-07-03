using System.Globalization;
using Warehouse.Contracts.Logistics;
using Warehouse.ServiceDefaults.BiExport;
using Wolverine;

namespace Warehouse.Logistics.Core.Application.BiExport;

/// <summary>
/// Exports a copy of every <see cref="PickConfirmedV1"/> to the Bronze landing zone (docs/BI_Plan.md
/// §5/§11 — feeds picking metrics, §8). Same shape as Warehousing's
/// <c>GoodsReceivedBiExportConsumer</c>: a pure side observer on its own dedicated queue, no domain
/// writes, no outbox.
/// </summary>
public sealed class PickConfirmedBiExportConsumer(IBronzeExportWriter writer)
{
    private const string SourceSystem = "logistics-service";

    public Task Handle(PickConfirmedV1 message, Envelope envelope, CancellationToken cancellationToken)
    {
        var bronzeEvent = new BronzeEvent(
            SourceSystem,
            EventType: nameof(PickConfirmedV1),
            EventId: envelope.Id.ToString(), // Wolverine's own message id — stable across redeliveries.
            message.OccurredAt,
            Payload: new Dictionary<string, string?>
            {
                ["order_id"] = message.OrderId.ToString(),
                ["sequence"] = message.Sequence.ToString(CultureInfo.InvariantCulture),
                ["location"] = message.Location,
                ["sku"] = message.Sku,
                ["batch_number"] = message.BatchNumber,
                ["quantity"] = message.Quantity.ToString(CultureInfo.InvariantCulture),
                ["unit"] = message.Unit,
            });

        return writer.WriteAsync(bronzeEvent, cancellationToken);
    }
}
