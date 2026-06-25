using Warehouse.SharedKernel.Application;
using Warehouse.SharedKernel.Domain;
using Warehouse.Warehousing.Inventory.Application.Abstractions;
using Warehouse.Warehousing.Inventory.Domain;
using Warehouse.Warehousing.Inventory.Domain.Services;

namespace Warehouse.Warehousing.Inventory.Application.AdjustStockStatus;

/// <summary>
/// Desk row actions on a stock item (admin Stock screen): relocate it, or block it pending QC.
/// Both are real domain operations — a move produces one ledger entry via
/// <see cref="StockTransferService"/>; a block flips the status so the stock leaves allocation.
/// </summary>
public sealed record MoveStockCommand(Guid StockItemId, string ToLocation, string PerformedBy);

public sealed class MoveStockHandler(IStockItemRepository stockItems, IStockLedger ledger, IUnitOfWork unitOfWork)
{
    public async Task HandleAsync(MoveStockCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var source = await stockItems.GetByIdAsync(new StockItemId(command.StockItemId), cancellationToken)
            ?? throw new DomainException("stock_item_not_found", $"Stock item {command.StockItemId} does not exist.");

        var target = LocationCode.Of(command.ToLocation);
        var quantity = source.OnHand.Subtract(source.Allocated);
        if (quantity.IsZero)
        {
            throw new DomainException(
                "stock_move_fully_allocated",
                $"Stock {source.Sku} at {source.Location} is fully allocated — release allocations before moving.");
        }

        var destination = await stockItems.GetAtAsync(source.Sku, source.Batch, target, cancellationToken);
        if (destination is null)
        {
            destination = StockItem.CreateAt(target, source.Sku, source.Batch, source.OnHand.Unit);
            stockItems.Add(destination);
        }

        var movement = StockTransferService.Transfer(
            source, destination, quantity, MovementType.Move, command.PerformedBy, reason: "Manual move (desk)");
        ledger.Append(movement);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

public sealed record BlockStockCommand(Guid StockItemId, string Reason);

public sealed class BlockStockHandler(IStockItemRepository stockItems, IUnitOfWork unitOfWork)
{
    public async Task HandleAsync(BlockStockCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var item = await stockItems.GetByIdAsync(new StockItemId(command.StockItemId), cancellationToken)
            ?? throw new DomainException("stock_item_not_found", $"Stock item {command.StockItemId} does not exist.");

        item.MarkBlocked();
        stockItems.Update(item);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
