using NSubstitute;
using Warehouse.Contracts.Logistics;
using Warehouse.Logistics.Core.Application.BiExport;
using Warehouse.ServiceDefaults.BiExport;
using Wolverine;
using Xunit;

namespace Warehouse.Logistics.Tests.BiExport;

public sealed class GoodsReceiptConfirmedBiExportConsumerTests
{
    private static GoodsReceiptConfirmedV1 Confirmed() => new(
        DeliveryId: Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001"),
        WarehouseCode: "WH-01",
        Lines:
        [
            new GoodsReceiptLineV1(1, "MILK", 10m, "pcs", "B1", null, "None"),
            new GoodsReceiptLineV1(2, "RICE", 5m, "kg", null, null, "None"),
        ],
        OccurredAt: new DateTimeOffset(2026, 7, 3, 10, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task Maps_the_message_onto_a_BronzeEvent_with_the_line_count_and_a_JSON_payload()
    {
        var writer = Substitute.For<IBronzeExportWriter>();
        var consumer = new GoodsReceiptConfirmedBiExportConsumer(writer);
        var message = Confirmed();
        var envelope = new Envelope(message) { Id = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000002") };

        await consumer.Handle(message, envelope, CancellationToken.None);

        await writer.Received(1).WriteAsync(
            Arg.Is<BronzeEvent>(e =>
                e.SourceSystem == "logistics-service" &&
                e.EventType == nameof(GoodsReceiptConfirmedV1) &&
                e.EventId == "aaaaaaaa-0000-0000-0000-000000000002" &&
                e.OccurredAt == message.OccurredAt &&
                e.Payload["delivery_id"] == "aaaaaaaa-0000-0000-0000-000000000001" &&
                e.Payload["warehouse_code"] == "WH-01" &&
                e.Payload["line_count"] == "2" &&
                e.Payload["lines_json"]!.Contains("\"Sku\":\"MILK\"") &&
                e.Payload["lines_json"]!.Contains("\"Sku\":\"RICE\"")),
            Arg.Any<CancellationToken>());
    }
}
