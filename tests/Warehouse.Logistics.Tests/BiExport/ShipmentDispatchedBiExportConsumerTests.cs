using NSubstitute;
using Warehouse.Contracts.Logistics;
using Warehouse.Logistics.Core.Application.BiExport;
using Warehouse.ServiceDefaults.BiExport;
using Wolverine;
using Xunit;

namespace Warehouse.Logistics.Tests.BiExport;

public sealed class ShipmentDispatchedBiExportConsumerTests
{
    [Fact]
    public async Task Maps_the_message_onto_a_BronzeEvent_keyed_by_the_envelope_id()
    {
        var writer = Substitute.For<IBronzeExportWriter>();
        var consumer = new ShipmentDispatchedBiExportConsumer(writer);
        var message = new ShipmentDispatchedV1(
            OrderId: Guid.Parse("dddddddd-0000-0000-0000-000000000001"),
            ShipmentId: Guid.Parse("dddddddd-0000-0000-0000-000000000002"),
            WarehouseCode: "WH-01",
            CarrierRoleId: "carrier-7",
            TrackingNumber: "TRK-123",
            OccurredAt: new DateTimeOffset(2026, 7, 3, 10, 0, 0, TimeSpan.Zero));
        var envelope = new Envelope(message) { Id = Guid.Parse("dddddddd-0000-0000-0000-000000000003") };

        await consumer.Handle(message, envelope, CancellationToken.None);

        await writer.Received(1).WriteAsync(
            Arg.Is<BronzeEvent>(e =>
                e.SourceSystem == "logistics-service" &&
                e.EventType == nameof(ShipmentDispatchedV1) &&
                e.EventId == "dddddddd-0000-0000-0000-000000000003" &&
                e.Payload["order_id"] == "dddddddd-0000-0000-0000-000000000001" &&
                e.Payload["shipment_id"] == "dddddddd-0000-0000-0000-000000000002" &&
                e.Payload["warehouse_code"] == "WH-01" &&
                e.Payload["carrier_role_id"] == "carrier-7" &&
                e.Payload["tracking_number"] == "TRK-123"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_missing_tracking_number_carries_a_null_column()
    {
        var writer = Substitute.For<IBronzeExportWriter>();
        var consumer = new ShipmentDispatchedBiExportConsumer(writer);
        var message = new ShipmentDispatchedV1(
            Guid.NewGuid(), Guid.NewGuid(), "WH-01", "carrier-7", null,
            new DateTimeOffset(2026, 7, 3, 10, 0, 0, TimeSpan.Zero));
        var envelope = new Envelope(message) { Id = Guid.NewGuid() };

        await consumer.Handle(message, envelope, CancellationToken.None);

        await writer.Received(1).WriteAsync(
            Arg.Is<BronzeEvent>(e => e.Payload["tracking_number"] == null),
            Arg.Any<CancellationToken>());
    }
}
