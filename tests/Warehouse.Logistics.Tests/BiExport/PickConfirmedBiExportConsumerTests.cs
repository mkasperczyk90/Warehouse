using NSubstitute;
using Warehouse.Contracts.Logistics;
using Warehouse.Logistics.Core.Application.BiExport;
using Warehouse.ServiceDefaults.BiExport;
using Wolverine;
using Xunit;

namespace Warehouse.Logistics.Tests.BiExport;

public sealed class PickConfirmedBiExportConsumerTests
{
    [Fact]
    public async Task Maps_the_message_onto_a_BronzeEvent_keyed_by_the_envelope_id()
    {
        var writer = Substitute.For<IBronzeExportWriter>();
        var consumer = new PickConfirmedBiExportConsumer(writer);
        var message = new PickConfirmedV1(
            OrderId: Guid.Parse("ffffffff-0000-0000-0000-000000000001"),
            Sequence: 3,
            Location: "A-01-01",
            Sku: "MILK",
            BatchNumber: "B1",
            Quantity: 2.5m,
            Unit: "pcs",
            OccurredAt: new DateTimeOffset(2026, 7, 3, 10, 0, 0, TimeSpan.Zero));
        var envelope = new Envelope(message) { Id = Guid.Parse("ffffffff-0000-0000-0000-000000000002") };

        await consumer.Handle(message, envelope, CancellationToken.None);

        await writer.Received(1).WriteAsync(
            Arg.Is<BronzeEvent>(e =>
                e.SourceSystem == "logistics-service" &&
                e.EventType == nameof(PickConfirmedV1) &&
                e.EventId == "ffffffff-0000-0000-0000-000000000002" &&
                e.Payload["order_id"] == "ffffffff-0000-0000-0000-000000000001" &&
                e.Payload["sequence"] == "3" &&
                e.Payload["location"] == "A-01-01" &&
                e.Payload["sku"] == "MILK" &&
                e.Payload["batch_number"] == "B1" &&
                e.Payload["quantity"] == "2.5" &&
                e.Payload["unit"] == "pcs"),
            Arg.Any<CancellationToken>());
    }
}
