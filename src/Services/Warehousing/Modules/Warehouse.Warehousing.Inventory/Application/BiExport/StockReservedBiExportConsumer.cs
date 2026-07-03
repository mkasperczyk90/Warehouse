using Wolverine;
using Warehouse.Contracts.Logistics;
using Warehouse.ServiceDefaults.BiExport;

namespace Warehouse.Warehousing.Inventory.Application.BiExport;

/// <summary>
/// Exports a copy of every <see cref="StockReservedV1"/> to the Bronze landing zone (docs/BI_Plan.md
/// §5/§11 — feeds <c>FactReservation</c>/reservation fulfillment rate, §7/§8). Same shape as
/// <see cref="GoodsReceivedBiExportConsumer"/>: a pure side observer on the shared
/// <c>warehousing.bi-export</c> queue, no domain writes.
/// </summary>
public sealed class StockReservedBiExportConsumer(IBronzeExportWriter writer)
{
    private const string SourceSystem = "warehouse-service";

    public Task Handle(StockReservedV1 message, Envelope envelope, CancellationToken cancellationToken)
    {
        var bronzeEvent = new BronzeEvent(
            SourceSystem,
            EventType: nameof(StockReservedV1),
            EventId: envelope.Id.ToString(), // Wolverine's own message id — stable across redeliveries.
            message.OccurredAt,
            Payload: new Dictionary<string, string?>
            {
                ["order_id"] = message.OrderId.ToString(),
                ["fully"] = message.Fully.ToString(),
            });

        return writer.WriteAsync(bronzeEvent, cancellationToken);
    }
}
