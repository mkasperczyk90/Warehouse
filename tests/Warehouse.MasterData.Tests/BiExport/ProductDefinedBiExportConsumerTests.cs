using NSubstitute;
using Warehouse.Contracts.Catalog;
using Warehouse.MasterData.Catalog.Application.BiExport;
using Warehouse.ServiceDefaults.BiExport;
using Wolverine;
using Xunit;

namespace Warehouse.MasterData.Tests.BiExport;

public sealed class ProductDefinedBiExportConsumerTests
{
    private static ProductDefinedV2 Defined(decimal? min = 2, decimal? max = 6) => new(
        Sku: "MILK",
        Name: "Milk 1L",
        BaseUnit: "pcs",
        RequiresColdChain: min is not null,
        IsHazardous: false,
        IsBatchTracked: true,
        UnitWeightKg: 1.03m,
        UnitVolumeM3: 0.001m,
        MinCelsius: min,
        MaxCelsius: max,
        OccurredAt: new DateTimeOffset(2026, 7, 3, 10, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task Maps_the_message_onto_a_BronzeEvent_keyed_by_the_envelope_id()
    {
        var writer = Substitute.For<IBronzeExportWriter>();
        var consumer = new ProductDefinedBiExportConsumer(writer);
        var message = Defined();
        var envelope = new Envelope(message) { Id = Guid.Parse("11111111-2222-3333-4444-555555555555") };

        await consumer.Handle(message, envelope, CancellationToken.None);

        await writer.Received(1).WriteAsync(
            Arg.Is<BronzeEvent>(e =>
                e.SourceSystem == "masterdata-service" &&
                e.EventType == nameof(ProductDefinedV2) &&
                e.EventId == "11111111-2222-3333-4444-555555555555" &&
                e.OccurredAt == message.OccurredAt &&
                e.Payload["sku"] == "MILK" &&
                e.Payload["name"] == "Milk 1L" &&
                e.Payload["base_unit"] == "pcs" &&
                e.Payload["requires_cold_chain"] == "True" &&
                e.Payload["is_hazardous"] == "False" &&
                e.Payload["is_batch_tracked"] == "True" &&
                e.Payload["unit_weight_kg"] == "1.03" &&
                e.Payload["unit_volume_m3"] == "0.001" &&
                e.Payload["min_celsius"] == "2" &&
                e.Payload["max_celsius"] == "6"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task An_ambient_product_carries_null_temperature_columns()
    {
        var writer = Substitute.For<IBronzeExportWriter>();
        var consumer = new ProductDefinedBiExportConsumer(writer);
        var message = Defined(min: null, max: null);
        var envelope = new Envelope(message) { Id = Guid.NewGuid() };

        await consumer.Handle(message, envelope, CancellationToken.None);

        await writer.Received(1).WriteAsync(
            Arg.Is<BronzeEvent>(e =>
                e.Payload["min_celsius"] == null &&
                e.Payload["max_celsius"] == null &&
                e.Payload["requires_cold_chain"] == "False"),
            Arg.Any<CancellationToken>());
    }
}
