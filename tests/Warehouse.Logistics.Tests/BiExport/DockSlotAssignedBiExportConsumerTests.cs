using NSubstitute;
using Warehouse.Contracts.Logistics;
using Warehouse.Logistics.Core.Application.BiExport;
using Warehouse.ServiceDefaults.BiExport;
using Wolverine;
using Xunit;

namespace Warehouse.Logistics.Tests.BiExport;

public sealed class DockSlotAssignedBiExportConsumerTests
{
    [Fact]
    public async Task Maps_the_message_onto_a_BronzeEvent_keyed_by_the_envelope_id()
    {
        var writer = Substitute.For<IBronzeExportWriter>();
        var consumer = new DockSlotAssignedBiExportConsumer(writer);
        var message = new DockSlotAssignedV1(
            DeliveryId: Guid.Parse("dddddddd-0000-0000-0000-000000000001"),
            WarehouseCode: "WH-01",
            DockCode: "D-1",
            SlotFrom: new DateTimeOffset(2026, 7, 4, 8, 0, 0, TimeSpan.Zero),
            SlotTo: new DateTimeOffset(2026, 7, 4, 9, 0, 0, TimeSpan.Zero),
            OccurredAt: new DateTimeOffset(2026, 7, 3, 10, 0, 0, TimeSpan.Zero));
        var envelope = new Envelope(message) { Id = Guid.Parse("dddddddd-0000-0000-0000-000000000002") };

        await consumer.Handle(message, envelope, CancellationToken.None);

        await writer.Received(1).WriteAsync(
            Arg.Is<BronzeEvent>(e =>
                e.SourceSystem == "logistics-service" &&
                e.EventType == nameof(DockSlotAssignedV1) &&
                e.EventId == "dddddddd-0000-0000-0000-000000000002" &&
                e.Payload["delivery_id"] == "dddddddd-0000-0000-0000-000000000001" &&
                e.Payload["warehouse_code"] == "WH-01" &&
                e.Payload["dock_code"] == "D-1" &&
                e.Payload["dock_slot_at"] == message.SlotFrom.ToString("O") &&
                e.Payload["dock_slot_to"] == message.SlotTo.ToString("O")),
            Arg.Any<CancellationToken>());
    }
}
