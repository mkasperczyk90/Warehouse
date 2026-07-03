using NSubstitute;
using Warehouse.Contracts.Logistics;
using Warehouse.Logistics.Core.Application.BiExport;
using Warehouse.ServiceDefaults.BiExport;
using Wolverine;
using Xunit;

namespace Warehouse.Logistics.Tests.BiExport;

public sealed class OutboundOrderPlacedBiExportConsumerTests
{
    private static OutboundOrderPlacedV1 Placed() => new(
        OrderId: Guid.Parse("bbbbbbbb-0000-0000-0000-000000000001"),
        CustomerRoleId: "customer-42",
        WarehouseCode: "WH-01",
        Lines: [new OutboundOrderLineV1(1, "MILK", 3m, "pcs")],
        OccurredAt: new DateTimeOffset(2026, 7, 3, 10, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task Maps_the_message_onto_a_BronzeEvent_with_the_line_count_and_a_JSON_payload()
    {
        var writer = Substitute.For<IBronzeExportWriter>();
        var consumer = new OutboundOrderPlacedBiExportConsumer(writer);
        var message = Placed();
        var envelope = new Envelope(message) { Id = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002") };

        await consumer.Handle(message, envelope, CancellationToken.None);

        await writer.Received(1).WriteAsync(
            Arg.Is<BronzeEvent>(e =>
                e.SourceSystem == "logistics-service" &&
                e.EventType == nameof(OutboundOrderPlacedV1) &&
                e.EventId == "bbbbbbbb-0000-0000-0000-000000000002" &&
                e.Payload["order_id"] == "bbbbbbbb-0000-0000-0000-000000000001" &&
                e.Payload["customer_role_id"] == "customer-42" &&
                e.Payload["warehouse_code"] == "WH-01" &&
                e.Payload["line_count"] == "1" &&
                e.Payload["lines_json"]!.Contains("\"Sku\":\"MILK\"")),
            Arg.Any<CancellationToken>());
    }
}
