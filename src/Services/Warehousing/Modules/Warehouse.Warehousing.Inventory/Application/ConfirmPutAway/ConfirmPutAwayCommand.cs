using Warehouse.Contracts.Logistics;
using Warehouse.SharedKernel.Domain;
using Warehouse.SharedKernel.ValueObjects;
using Warehouse.Warehousing.Inventory.Application.Abstractions;
using Warehouse.Warehousing.Inventory.Domain;
using Warehouse.Warehousing.Inventory.Domain.Services;
using Warehouse.Warehousing.Inventory.Infrastructure.Persistence;
using Wolverine.EntityFrameworkCore;

namespace Warehouse.Warehousing.Inventory.Application.ConfirmPutAway;

/// <summary>
/// UC-04 — the operator scanned a buffer item and a target location: move the stock out of the dock
/// buffer into storage (one <c>PutAway</c> ledger entry). When the buffer is emptied, reply to
/// Logistics with <see cref="PutAwayCompletedV1"/> so the delivery completes.
/// </summary>
public sealed record ConfirmPutAwayCommand(
    Guid DeliveryId,
    string WarehouseCode,
    string Sku,
    string? BatchNumber,
    decimal Quantity,
    string Unit,
    string ToLocation,
    string PerformedBy);

public sealed class ConfirmPutAwayHandler(
    IStockItemRepository stockItems,
    IStockLedger ledger,
    IDbContextOutbox<InventoryDbContext> outbox)
{
    public async Task HandleAsync(ConfirmPutAwayCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var buffer = DockBuffer.For(WarehouseCode.Of(command.WarehouseCode));
        var sku = Sku.Of(command.Sku);
        var batch = command.BatchNumber is { } number ? BatchNumber.Of(number) : null;
        var unit = UnitOfMeasure.FromCode(command.Unit);
        var quantity = Quantity.Of(command.Quantity, unit);
        var target = LocationCode.Of(command.ToLocation);

        // Load the whole buffer (tracked) so we can both find the source and tell when it is emptied.
        var bufferItems = await stockItems.ListAtAsync(buffer, cancellationToken);
        var source = bufferItems.FirstOrDefault(i =>
                         i.Sku == sku && (batch is null ? i.Batch is null : i.Batch == batch))
            ?? throw new DomainException(
                "put_away_no_buffer_stock",
                $"No {sku} {(batch is null ? string.Empty : batch + " ")}in the dock buffer of {command.WarehouseCode}.");

        var destination = await stockItems.GetAtAsync(sku, batch, target, cancellationToken);
        if (destination is null)
        {
            destination = StockItem.CreateAt(target, sku, batch, unit);
            stockItems.Add(destination);
        }

        var movement = StockTransferService.Transfer(
            source, destination, quantity, MovementType.PutAway,
            command.PerformedBy, reason: $"Delivery {command.DeliveryId}");
        ledger.Append(movement);

        // Heuristic completion: when nothing is left in the buffer, the delivery is fully put away.
        // (Per-delivery tracking of buffer stock is deferred; the dev/e2e flow handles one delivery.)
        if (bufferItems.All(i => i.OnHand.IsZero))
        {
            await outbox.PublishAsync(new PutAwayCompletedV1(command.DeliveryId, DateTimeOffset.UtcNow));
        }

        await outbox.SaveChangesAndFlushMessagesAsync(cancellationToken);
    }
}
