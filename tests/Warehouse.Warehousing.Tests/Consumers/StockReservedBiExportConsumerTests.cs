using NSubstitute;
using Warehouse.Contracts.Logistics;
using Warehouse.ServiceDefaults.BiExport;
using Warehouse.Warehousing.Inventory.Application.BiExport;
using Wolverine;
using Xunit;

namespace Warehouse.Warehousing.Tests.Consumers;

public sealed class StockReservedBiExportConsumerTests
{
    private static StockReservedV1 Reserved(bool fully = true) => new(
        OrderId: Guid.Parse("66666666-6666-6666-6666-666666666666"),
        Fully: fully,
        OccurredAt: new DateTimeOffset(2026, 7, 3, 10, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task Maps_the_message_onto_a_BronzeEvent_keyed_by_the_envelope_id()
    {
        var writer = Substitute.For<IBronzeExportWriter>();
        var consumer = new StockReservedBiExportConsumer(writer);
        var message = Reserved();
        var envelope = new Envelope(message) { Id = Guid.Parse("77777777-7777-7777-7777-777777777777") };

        await consumer.Handle(message, envelope, CancellationToken.None);

        await writer.Received(1).WriteAsync(
            Arg.Is<BronzeEvent>(e =>
                e.SourceSystem == "warehouse-service" &&
                e.EventType == nameof(StockReservedV1) &&
                e.EventId == "77777777-7777-7777-7777-777777777777" &&
                e.OccurredAt == message.OccurredAt &&
                e.Payload["order_id"] == "66666666-6666-6666-6666-666666666666" &&
                e.Payload["fully"] == "True"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_partial_reservation_carries_Fully_False()
    {
        var writer = Substitute.For<IBronzeExportWriter>();
        var consumer = new StockReservedBiExportConsumer(writer);
        var message = Reserved(fully: false);
        var envelope = new Envelope(message) { Id = Guid.NewGuid() };

        await consumer.Handle(message, envelope, CancellationToken.None);

        await writer.Received(1).WriteAsync(
            Arg.Is<BronzeEvent>(e => e.Payload["fully"] == "False"),
            Arg.Any<CancellationToken>());
    }
}
