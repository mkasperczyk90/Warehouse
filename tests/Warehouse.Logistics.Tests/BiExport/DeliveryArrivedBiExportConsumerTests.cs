using NSubstitute;
using Warehouse.Contracts.Logistics;
using Warehouse.Logistics.Core.Application.BiExport;
using Warehouse.ServiceDefaults.BiExport;
using Wolverine;
using Xunit;

namespace Warehouse.Logistics.Tests.BiExport;

public sealed class DeliveryArrivedBiExportConsumerTests
{
    [Fact]
    public async Task Maps_the_message_onto_a_BronzeEvent_keyed_by_the_envelope_id()
    {
        var writer = Substitute.For<IBronzeExportWriter>();
        var consumer = new DeliveryArrivedBiExportConsumer(writer);
        var message = new DeliveryArrivedV1(
            DeliveryId: Guid.Parse("dddddddd-0000-0000-0000-000000000001"),
            WarehouseCode: "WH-01",
            OccurredAt: new DateTimeOffset(2026, 7, 3, 10, 0, 0, TimeSpan.Zero));
        var envelope = new Envelope(message) { Id = Guid.Parse("dddddddd-0000-0000-0000-000000000002") };

        await consumer.Handle(message, envelope, CancellationToken.None);

        await writer.Received(1).WriteAsync(
            Arg.Is<BronzeEvent>(e =>
                e.SourceSystem == "logistics-service" &&
                e.EventType == nameof(DeliveryArrivedV1) &&
                e.EventId == "dddddddd-0000-0000-0000-000000000002" &&
                e.Payload["delivery_id"] == "dddddddd-0000-0000-0000-000000000001" &&
                e.Payload["warehouse_code"] == "WH-01" &&
                e.Payload["arrived_at"] == message.OccurredAt.ToString("O")),
            Arg.Any<CancellationToken>());
    }
}
