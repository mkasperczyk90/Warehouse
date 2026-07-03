using System.Globalization;
using Wolverine;
using Warehouse.Contracts.Logistics;
using Warehouse.ServiceDefaults.BiExport;

namespace Warehouse.Warehousing.Inventory.Application.BiExport;

/// <summary>
/// Exports a copy of every <see cref="GoodsReceivedV1"/> to the Bronze landing zone
/// (docs/BI_Plan.md §5/§11 — feeds <c>FactInboundReceipt</c>, §7). Purely a side observer of an
/// event Inventory already publishes: it makes no domain writes, participates in no aggregate
/// transaction, and cannot affect <see cref="GoodsReceivedV1"/>'s own delivery to Logistics — if
/// the BI lake is unreachable, <see cref="IBronzeExportWriter"/> is still resolved (as
/// <see cref="NullBronzeExportWriter"/> when export is disabled, see
/// <c>AddBiExport</c>), so this consumer never blocks or fails the business flow.
/// <para>
/// <b>Reference implementation.</b> To export another Inventory-published event
/// (<c>PutAwayCompletedV1</c>, <c>StockReservedV1</c>, ...), add another small class with this exact
/// shape — a constructor-injected <see cref="IBronzeExportWriter"/> and a <c>Handle</c> method that
/// maps the event's fields into a <see cref="BronzeEvent"/>. Wolverine discovers it automatically
/// (convention-based, see <c>opts.Discovery.IncludeAssembly</c> in Program.cs); nothing else changes.
/// The reusable machinery (the writer, its config, its DI registration) lives in
/// <c>Warehouse.ServiceDefaults.BiExport</c> and is shared by every service, not just Inventory.
/// </para>
/// </summary>
public sealed class GoodsReceivedBiExportConsumer(IBronzeExportWriter writer)
{
    private const string SourceSystem = "warehouse-service";

    public Task Handle(GoodsReceivedV1 message, Envelope envelope, CancellationToken cancellationToken)
    {
        var @event = new BronzeEvent(
            SourceSystem,
            EventType: nameof(GoodsReceivedV1),
            EventId: envelope.Id.ToString(), // Wolverine's own message id — stable across redeliveries.
            message.OccurredAt,
            Payload: new Dictionary<string, string?>
            {
                ["delivery_id"] = message.DeliveryId.ToString(),
                ["warehouse_code"] = message.WarehouseCode,
                ["buffer_location"] = message.BufferLocation,
                ["line_count"] = message.LineCount.ToString(CultureInfo.InvariantCulture),
            });

        return writer.WriteAsync(@event, cancellationToken);
    }
}
