using Warehouse.Contracts.Logistics;
using Warehouse.SharedKernel.ValueObjects;
using Warehouse.Warehousing.Inventory.Application.Abstractions;
using Warehouse.Warehousing.Inventory.Domain;
using Warehouse.Warehousing.Inventory.Infrastructure.Persistence;
using Wolverine.EntityFrameworkCore;

namespace Warehouse.Warehousing.Inventory.Application.ReceiveGoodsReceipt;

/// <summary>
/// UC-02 (Inventory side): reacts to Logistics' <see cref="GoodsReceiptConfirmedV1"/> by bringing
/// each received line on stock in the warehouse's dock buffer, recording a <c>GoodsReceipt</c>
/// movement in the ledger, and replying with <see cref="GoodsReceivedV1"/> so Logistics can start
/// put-away. Stock, ledger and the reply commit in one transaction through the outbox.
/// </summary>
public sealed class ReceiveGoodsReceiptConsumer(
    IStockItemRepository stockItems,
    IStockLedger ledger,
    IDbContextOutbox<InventoryDbContext> outbox)
{
    public async Task Handle(GoodsReceiptConfirmedV1 message, CancellationToken cancellationToken)
    {
        var warehouse = WarehouseCode.Of(message.WarehouseCode);
        var buffer = DockBuffer.For(warehouse);

        foreach (var line in message.Lines)
        {
            if (line.Quantity <= 0)
            {
                continue;
            }

            var sku = Sku.Of(line.Sku);
            var batch = line.BatchNumber is { } number ? BatchNumber.Of(number) : null;
            var unit = UnitOfMeasure.FromCode(line.Unit);
            var quantity = Quantity.Of(line.Quantity, unit);

            var stockItem = await stockItems.GetAtAsync(sku, batch, buffer, cancellationToken);
            if (stockItem is null)
            {
                stockItem = StockItem.CreateAt(buffer, sku, batch, unit);
                stockItems.Add(stockItem);
            }

            var movement = stockItem.Receive(
                quantity, MovementType.GoodsReceipt, performedBy: "goods-receipt",
                reason: $"Delivery {message.DeliveryId}");
            ledger.Append(movement);
        }

        await outbox.PublishAsync(new GoodsReceivedV1(
            message.DeliveryId, warehouse.Value, buffer.Value, message.Lines.Count, DateTimeOffset.UtcNow));

        await outbox.SaveChangesAndFlushMessagesAsync(cancellationToken);
    }
}
