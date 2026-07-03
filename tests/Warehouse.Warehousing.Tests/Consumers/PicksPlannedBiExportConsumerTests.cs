using NSubstitute;
using Warehouse.Contracts.Logistics;
using Warehouse.ServiceDefaults.BiExport;
using Warehouse.Warehousing.Inventory.Application.BiExport;
using Wolverine;
using Xunit;

namespace Warehouse.Warehousing.Tests.Consumers;

public sealed class PicksPlannedBiExportConsumerTests
{
    private static PicksPlannedV1 Planned() => new(
        OrderId: Guid.Parse("88888888-8888-8888-8888-888888888888"),
        Picks:
        [
            new PlannedPickV1("A-01-01", "MILK", "B1", 10m, "pcs"),
            new PlannedPickV1("A-01-02", "RICE", null, 5m, "kg"),
        ],
        OccurredAt: new DateTimeOffset(2026, 7, 3, 10, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task Maps_the_message_onto_a_BronzeEvent_with_the_pick_count_and_a_JSON_payload()
    {
        var writer = Substitute.For<IBronzeExportWriter>();
        var consumer = new PicksPlannedBiExportConsumer(writer);
        var message = Planned();
        var envelope = new Envelope(message) { Id = Guid.Parse("99999999-9999-9999-9999-999999999999") };

        await consumer.Handle(message, envelope, CancellationToken.None);

        await writer.Received(1).WriteAsync(
            Arg.Is<BronzeEvent>(e =>
                e.SourceSystem == "warehouse-service" &&
                e.EventType == nameof(PicksPlannedV1) &&
                e.EventId == "99999999-9999-9999-9999-999999999999" &&
                e.OccurredAt == message.OccurredAt &&
                e.Payload["order_id"] == "88888888-8888-8888-8888-888888888888" &&
                e.Payload["pick_count"] == "2" &&
                e.Payload["picks_json"]!.Contains("\"Sku\":\"MILK\"") &&
                e.Payload["picks_json"]!.Contains("\"Sku\":\"RICE\"")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task An_order_with_no_picks_carries_a_zero_count_and_an_empty_array()
    {
        var writer = Substitute.For<IBronzeExportWriter>();
        var consumer = new PicksPlannedBiExportConsumer(writer);
        var message = Planned() with { Picks = [] };
        var envelope = new Envelope(message) { Id = Guid.NewGuid() };

        await consumer.Handle(message, envelope, CancellationToken.None);

        await writer.Received(1).WriteAsync(
            Arg.Is<BronzeEvent>(e => e.Payload["pick_count"] == "0" && e.Payload["picks_json"] == "[]"),
            Arg.Any<CancellationToken>());
    }
}
