using Warehouse.SharedKernel.Application;
using Warehouse.SharedKernel.Domain;
using Warehouse.SharedKernel.ValueObjects;
using Warehouse.Warehousing.Inventory.Application.Abstractions;
using Warehouse.Warehousing.Inventory.Domain;

namespace Warehouse.Warehousing.Inventory.Application.AdjustStock;

/// <summary>
/// UC-08 — manual stock correction (damage, loss, count fix). Drives the on-hand to a counted figure via
/// <see cref="StockItem.AdjustTo"/>, which is the only way to change stock without a physical movement and
/// always writes one signed ledger entry (in or out). The domain guards the invariants: stock can't go
/// below zero or below what's allocated, and a no-op is rejected. The posted entry is irreversible — a
/// later mistake is fixed with another adjustment, never an edit (ADR-0002).
/// </summary>
public sealed record AdjustStockCommand(
    Guid StockItemId,
    decimal NewQuantity,
    string Reason,
    string? Note,
    string PerformedBy);

public sealed record AdjustStockResult(
    string ItemId,
    decimal NewOnHand,
    decimal Delta,
    string Reason,
    string PostedBy,
    DateTimeOffset PostedAt);

public sealed class AdjustStockHandler(IStockItemRepository stockItems, IStockLedger ledger, IUnitOfWork unitOfWork)
{
    public async Task<AdjustStockResult> HandleAsync(
        AdjustStockCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var item = await stockItems.GetByIdAsync(new StockItemId(command.StockItemId), cancellationToken)
            ?? throw new DomainException("stock_item_not_found", $"Stock item {command.StockItemId} does not exist.");

        var before = item.OnHand.Amount;
        var newOnHand = Quantity.Of(command.NewQuantity, item.OnHand.Unit);
        var reason = string.IsNullOrWhiteSpace(command.Note)
            ? command.Reason
            : $"{command.Reason}: {command.Note}";

        var movement = item.AdjustTo(newOnHand, reason, command.PerformedBy);
        ledger.Append(movement);
        stockItems.Update(item);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new AdjustStockResult(
            command.StockItemId.ToString(),
            item.OnHand.Amount,
            item.OnHand.Amount - before,
            command.Reason,
            command.PerformedBy,
            movement.OccurredAt);
    }
}
