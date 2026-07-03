using NSubstitute;
using Warehouse.Contracts.Logistics;
using Warehouse.ServiceDefaults.BiExport;
using Warehouse.Warehousing.Inventory.Application.BiExport;
using Wolverine;
using Xunit;

namespace Warehouse.Warehousing.Tests.Consumers;

/// <summary>
/// <see cref="GoodsReceivedBiExportConsumer"/> is a pure side observer — it never touches a
/// repository or the outbox, only maps the event onto a <see cref="BronzeEvent"/> and hands it to
/// <see cref="IBronzeExportWriter"/> (docs/BI_Plan.md §5/§11).
/// </summary>
public sealed class GoodsReceivedBiExportConsumerTests
{
    private static GoodsReceivedV1 Received() => new(
        DeliveryId: Guid.Parse("22222222-2222-2222-2222-222222222222"),
        WarehouseCode: "WH-01",
        BufferLocation: "DOCK-1",
        LineCount: 3,
        OccurredAt: new DateTimeOffset(2026, 7, 3, 10, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task Maps_the_message_onto_a_BronzeEvent_keyed_by_the_envelope_id()
    {
        var writer = Substitute.For<IBronzeExportWriter>();
        var consumer = new GoodsReceivedBiExportConsumer(writer);
        var message = Received();
        var envelope = new Envelope(message) { Id = Guid.Parse("33333333-3333-3333-3333-333333333333") };

        await consumer.Handle(message, envelope, CancellationToken.None);

        await writer.Received(1).WriteAsync(
            Arg.Is<BronzeEvent>(e =>
                e.SourceSystem == "warehouse-service" &&
                e.EventType == nameof(GoodsReceivedV1) &&
                e.EventId == "33333333-3333-3333-3333-333333333333" &&
                e.OccurredAt == message.OccurredAt &&
                e.Payload["delivery_id"] == "22222222-2222-2222-2222-222222222222" &&
                e.Payload["warehouse_code"] == "WH-01" &&
                e.Payload["buffer_location"] == "DOCK-1" &&
                e.Payload["line_count"] == "3"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_redelivery_with_the_same_envelope_id_produces_the_same_EventId()
    {
        var writer = Substitute.For<IBronzeExportWriter>();
        var consumer = new GoodsReceivedBiExportConsumer(writer);
        var message = Received();
        var envelope = new Envelope(message) { Id = Guid.Parse("33333333-3333-3333-3333-333333333333") };

        await consumer.Handle(message, envelope, CancellationToken.None);
        await consumer.Handle(message, envelope, CancellationToken.None);

        await writer.Received(2).WriteAsync(
            Arg.Is<BronzeEvent>(e => e.EventId == "33333333-3333-3333-3333-333333333333"),
            Arg.Any<CancellationToken>());
    }
}
