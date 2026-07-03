using System.Globalization;
using Warehouse.Contracts.Catalog;
using Warehouse.ServiceDefaults.BiExport;
using Wolverine;

namespace Warehouse.MasterData.Catalog.Application.BiExport;

/// <summary>
/// Exports a copy of every <see cref="ProductDefinedV2"/> to the Bronze landing zone (docs/BI_Plan.md
/// §5/§11 — feeds <c>DimProduct</c>, §6/§7). Same shape as Warehousing's
/// <c>GoodsReceivedBiExportConsumer</c> (the original reference implementation): a pure side observer
/// on its own dedicated queue, no domain writes, no outbox.
/// </summary>
public sealed class ProductDefinedBiExportConsumer(IBronzeExportWriter writer)
{
    private const string SourceSystem = "masterdata-service";

    public Task Handle(ProductDefinedV2 message, Envelope envelope, CancellationToken cancellationToken)
    {
        var bronzeEvent = new BronzeEvent(
            SourceSystem,
            EventType: nameof(ProductDefinedV2),
            EventId: envelope.Id.ToString(), // Wolverine's own message id — stable across redeliveries.
            message.OccurredAt,
            Payload: new Dictionary<string, string?>
            {
                ["sku"] = message.Sku,
                ["name"] = message.Name,
                ["base_unit"] = message.BaseUnit,
                ["requires_cold_chain"] = message.RequiresColdChain.ToString(),
                ["is_hazardous"] = message.IsHazardous.ToString(),
                ["is_batch_tracked"] = message.IsBatchTracked.ToString(),
                ["unit_weight_kg"] = message.UnitWeightKg.ToString(CultureInfo.InvariantCulture),
                ["unit_volume_m3"] = message.UnitVolumeM3.ToString(CultureInfo.InvariantCulture),
                ["min_celsius"] = message.MinCelsius?.ToString(CultureInfo.InvariantCulture),
                ["max_celsius"] = message.MaxCelsius?.ToString(CultureInfo.InvariantCulture),
            });

        return writer.WriteAsync(bronzeEvent, cancellationToken);
    }
}
