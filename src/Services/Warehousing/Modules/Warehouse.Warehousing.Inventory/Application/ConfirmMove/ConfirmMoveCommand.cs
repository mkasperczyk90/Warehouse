using Warehouse.SharedKernel.Application;
using Warehouse.SharedKernel.Domain;
using Warehouse.SharedKernel.ValueObjects;
using Warehouse.Warehousing.Inventory.Application.Abstractions;
using Warehouse.Warehousing.Inventory.Application.Storage;
using Warehouse.Warehousing.Inventory.Domain;
using Warehouse.Warehousing.Inventory.Domain.Services;

namespace Warehouse.Warehousing.Inventory.Application.ConfirmMove;

/// <summary>
/// UC-06 — the operator confirmed a replenishment move: relocate the available stock of one item to the
/// scanned pick face, re-checking the hard storage invariant first (temperature / hazmat / capacity) and
/// posting a single <c>Move</c> ledger entry via <see cref="StockTransferService"/>. Allocated quantity
/// stays put — only free stock moves. The requested quantity is clamped to what is available.
/// </summary>
public sealed record ConfirmMoveCommand(Guid SourceItemId, string ToLocation, decimal Quantity, string PerformedBy);

public sealed class ConfirmMoveHandler(
    IStockItemRepository stockItems,
    StorageCompatibility compatibility,
    IStockLedger ledger,
    IUnitOfWork unitOfWork)
{
    public async Task HandleAsync(ConfirmMoveCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var source = await stockItems.GetByIdAsync(new StockItemId(command.SourceItemId), cancellationToken)
            ?? throw new DomainException("move_stock_item_not_found", $"Stock item {command.SourceItemId} does not exist.");

        var available = source.Available;
        if (available.IsZero)
        {
            throw new DomainException(
                "move_stock_unavailable",
                $"Stock {source.Sku} at {source.Location} has nothing free to move (blocked or fully allocated).");
        }

        // Honour the operator's chosen quantity, clamped to what is actually free at the source.
        var amount = command.Quantity > 0 && command.Quantity <= available.Amount ? command.Quantity : available.Amount;
        var quantity = Quantity.Of(amount, source.OnHand.Unit);

        var target = LocationCode.Of(command.ToLocation);
        await compatibility.EnsureCanStoreAsync(source.Sku, target, quantity, "move", cancellationToken);

        var destination = await stockItems.GetAtAsync(source.Sku, source.Batch, target, cancellationToken);
        if (destination is null)
        {
            destination = StockItem.CreateAt(target, source.Sku, source.Batch, source.OnHand.Unit);
            stockItems.Add(destination);
        }

        var movement = StockTransferService.Transfer(
            source, destination, quantity, MovementType.Move, command.PerformedBy, reason: "Replenishment");
        ledger.Append(movement);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
