using NSubstitute;
using Warehouse.Contracts.Logistics;
using Warehouse.Logistics.Core.Application.BiExport;
using Warehouse.ServiceDefaults.BiExport;
using Wolverine;
using Xunit;

namespace Warehouse.Logistics.Tests.BiExport;

public sealed class OutboundOrderCancelledBiExportConsumerTests
{
    [Fact]
    public async Task Maps_the_message_onto_a_BronzeEvent_keyed_by_the_envelope_id()
    {
        var writer = Substitute.For<IBronzeExportWriter>();
        var consumer = new OutboundOrderCancelledBiExportConsumer(writer);
        var message = new OutboundOrderCancelledV1(
            Guid.Parse("cccccccc-0000-0000-0000-000000000001"),
            new DateTimeOffset(2026, 7, 3, 10, 0, 0, TimeSpan.Zero));
        var envelope = new Envelope(message) { Id = Guid.Parse("cccccccc-0000-0000-0000-000000000002") };

        await consumer.Handle(message, envelope, CancellationToken.None);

        await writer.Received(1).WriteAsync(
            Arg.Is<BronzeEvent>(e =>
                e.SourceSystem == "logistics-service" &&
                e.EventType == nameof(OutboundOrderCancelledV1) &&
                e.EventId == "cccccccc-0000-0000-0000-000000000002" &&
                e.Payload["order_id"] == "cccccccc-0000-0000-0000-000000000001"),
            Arg.Any<CancellationToken>());
    }
}
