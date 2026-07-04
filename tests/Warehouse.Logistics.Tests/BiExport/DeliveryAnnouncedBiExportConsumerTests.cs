using NSubstitute;
using Warehouse.Contracts.Logistics;
using Warehouse.Logistics.Core.Application.BiExport;
using Warehouse.ServiceDefaults.BiExport;
using Wolverine;
using Xunit;

namespace Warehouse.Logistics.Tests.BiExport;

public sealed class DeliveryAnnouncedBiExportConsumerTests
{
    [Fact]
    public async Task Maps_the_message_onto_a_BronzeEvent_keyed_by_the_envelope_id()
    {
        var writer = Substitute.For<IBronzeExportWriter>();
        var consumer = new DeliveryAnnouncedBiExportConsumer(writer);
        var message = new DeliveryAnnouncedV1(
            DeliveryId: Guid.Parse("dddddddd-0000-0000-0000-000000000001"),
            WarehouseCode: "WH-01",
            SupplierRoleId: "supplier-7",
            PlannedAt: new DateTimeOffset(2026, 7, 4, 8, 0, 0, TimeSpan.Zero),
            OccurredAt: new DateTimeOffset(2026, 7, 3, 10, 0, 0, TimeSpan.Zero));
        var envelope = new Envelope(message) { Id = Guid.Parse("dddddddd-0000-0000-0000-000000000002") };

        await consumer.Handle(message, envelope, CancellationToken.None);

        await writer.Received(1).WriteAsync(
            Arg.Is<BronzeEvent>(e =>
                e.SourceSystem == "logistics-service" &&
                e.EventType == nameof(DeliveryAnnouncedV1) &&
                e.EventId == "dddddddd-0000-0000-0000-000000000002" &&
                e.Payload["delivery_id"] == "dddddddd-0000-0000-0000-000000000001" &&
                e.Payload["warehouse_code"] == "WH-01" &&
                e.Payload["supplier_party_id"] == "supplier-7" &&
                e.Payload["planned_at"] == message.PlannedAt.ToString("O")),
            Arg.Any<CancellationToken>());
    }
}
