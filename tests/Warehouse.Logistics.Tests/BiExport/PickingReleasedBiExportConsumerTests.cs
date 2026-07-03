using NSubstitute;
using Warehouse.Contracts.Logistics;
using Warehouse.Logistics.Core.Application.BiExport;
using Warehouse.ServiceDefaults.BiExport;
using Wolverine;
using Xunit;

namespace Warehouse.Logistics.Tests.BiExport;

public sealed class PickingReleasedBiExportConsumerTests
{
    [Fact]
    public async Task Maps_the_message_onto_a_BronzeEvent_keyed_by_the_envelope_id()
    {
        var writer = Substitute.For<IBronzeExportWriter>();
        var consumer = new PickingReleasedBiExportConsumer(writer);
        var message = new PickingReleasedV1(
            Guid.Parse("eeeeeeee-0000-0000-0000-000000000001"),
            "WH-01",
            new DateTimeOffset(2026, 7, 3, 10, 0, 0, TimeSpan.Zero));
        var envelope = new Envelope(message) { Id = Guid.Parse("eeeeeeee-0000-0000-0000-000000000002") };

        await consumer.Handle(message, envelope, CancellationToken.None);

        await writer.Received(1).WriteAsync(
            Arg.Is<BronzeEvent>(e =>
                e.SourceSystem == "logistics-service" &&
                e.EventType == nameof(PickingReleasedV1) &&
                e.EventId == "eeeeeeee-0000-0000-0000-000000000002" &&
                e.Payload["order_id"] == "eeeeeeee-0000-0000-0000-000000000001" &&
                e.Payload["warehouse_code"] == "WH-01"),
            Arg.Any<CancellationToken>());
    }
}
