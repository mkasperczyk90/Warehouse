using Warehouse.Contracts.Logistics;
using Warehouse.Logistics.Core.Application.Abstractions;
using Warehouse.Logistics.Core.Domain;
using Warehouse.Logistics.Core.Infrastructure.Persistence;
using Wolverine.EntityFrameworkCore;

namespace Warehouse.Logistics.Core.Application.Deliveries.ConfirmReceipt;

/// <summary>UC-02 — confirm the goods receipt for the whole delivery.</summary>
public sealed record ConfirmReceiptCommand(Guid DeliveryId);

/// <summary>
/// Confirms the receipt and announces it to Inventory via the transactional outbox: the
/// <see cref="GoodsReceiptConfirmedV1"/> event and the aggregate change commit as one transaction,
/// so the "publish after save" can never be lost or sent for a rolled-back receipt.
/// </summary>
public sealed class ConfirmReceiptHandler(
    IInboundDeliveryRepository deliveries,
    IDbContextOutbox<LogisticsDbContext> outbox)
{
    public async Task HandleAsync(ConfirmReceiptCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        var delivery = await deliveries.GetByIdAsync(new DeliveryId(command.DeliveryId), cancellationToken)
            ?? throw new KeyNotFoundException($"Delivery {command.DeliveryId} not found.");

        delivery.ConfirmReceipt();
        deliveries.Update(delivery);

        // Only lines that actually received something go on stock; pure shortages carry zero quantity.
        var lines = delivery.Lines
            .Where(l => l.Actual is not null && !l.Actual.IsZero)
            .Select(l => new GoodsReceiptLineV1(
                l.LineNo,
                l.Product.Value,
                l.Actual!.Amount,
                l.Actual.Unit.Code,
                l.Batch?.Number,
                l.Batch?.ExpiryDate,
                l.Discrepancy.ToString()))
            .ToList();

        await outbox.PublishAsync(new GoodsReceiptConfirmedV1(
            delivery.Id.Value, delivery.Warehouse.Code, lines, DateTimeOffset.UtcNow));

        await outbox.SaveChangesAndFlushMessagesAsync(cancellationToken);
    }
}
