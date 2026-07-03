using NSubstitute;
using Warehouse.Contracts.Logistics;
using Warehouse.ServiceDefaults.BiExport;
using Warehouse.Warehousing.Inventory.Application.BiExport;
using Wolverine;
using Xunit;

namespace Warehouse.Warehousing.Tests.Consumers;

public sealed class PutAwayCompletedBiExportConsumerTests
{
    private static PutAwayCompletedV1 Completed() => new(
        DeliveryId: Guid.Parse("44444444-4444-4444-4444-444444444444"),
        OccurredAt: new DateTimeOffset(2026, 7, 3, 10, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task Maps_the_message_onto_a_BronzeEvent_keyed_by_the_envelope_id()
    {
        var writer = Substitute.For<IBronzeExportWriter>();
        var consumer = new PutAwayCompletedBiExportConsumer(writer);
        var message = Completed();
        var envelope = new Envelope(message) { Id = Guid.Parse("55555555-5555-5555-5555-555555555555") };

        await consumer.Handle(message, envelope, CancellationToken.None);

        await writer.Received(1).WriteAsync(
            Arg.Is<BronzeEvent>(e =>
                e.SourceSystem == "warehouse-service" &&
                e.EventType == nameof(PutAwayCompletedV1) &&
                e.EventId == "55555555-5555-5555-5555-555555555555" &&
                e.OccurredAt == message.OccurredAt &&
                e.Payload["delivery_id"] == "44444444-4444-4444-4444-444444444444"),
            Arg.Any<CancellationToken>());
    }
}
