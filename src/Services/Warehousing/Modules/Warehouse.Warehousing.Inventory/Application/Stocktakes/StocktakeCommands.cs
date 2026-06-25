using Microsoft.EntityFrameworkCore;
using Warehouse.SharedKernel.Domain;
using Warehouse.Warehousing.Inventory.Application.Abstractions;
using Warehouse.Warehousing.Inventory.Domain;
using Warehouse.Warehousing.Inventory.Infrastructure.Persistence;

namespace Warehouse.Warehousing.Inventory.Application.Stocktakes;

/// <summary>
/// UC-07 writes: start a blind count over a warehouse's locations, and approve a counted one. Approval is
/// where stocktake meets the ledger — each accepted difference line is posted as a stock adjustment
/// (<see cref="StockItem.AdjustTo"/>), so the count reconciles the system to reality through the same
/// immutable ledger as every other movement. The operator's per-line reason rides along.
/// </summary>
public sealed record StartStocktakeCommand(string Warehouse, string Scope, string OrderedBy);

public sealed class StartStocktakeHandler(InventoryDbContext db)
{
    public async Task<Guid> HandleAsync(StartStocktakeCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var warehouse = WarehouseCode.Of(command.Warehouse);
        var locations = await db.LocationSnapshots.AsNoTracking()
            .ToListAsync(cancellationToken);
        var scope = locations
            .Where(l => l.Warehouse == warehouse)
            .Select(l => l.Code)
            .ToList();

        if (scope.Count == 0)
        {
            throw new DomainException(
                "stocktake_no_locations", $"Warehouse {command.Warehouse} has no known locations to count.");
        }

        // The blind count is created here; operators record the actual counts (terminal), the desk reviews.
        var stocktake = Stocktake.Order(scope, command.OrderedBy, command.Scope);
        db.Stocktakes.Add(stocktake);
        await db.SaveChangesAsync(cancellationToken);
        return stocktake.Id.Value;
    }
}

public sealed record ApproveLine(int LineIndex, string Reason);

public sealed record ApproveStocktakeCommand(Guid StocktakeId, IReadOnlyList<ApproveLine> Lines, string PerformedBy);

public sealed class ApproveStocktakeHandler(
    InventoryDbContext db, IStockItemRepository stockItems, IStockLedger ledger)
{
    public async Task HandleAsync(ApproveStocktakeCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var stocktake = await db.Stocktakes
            .FirstOrDefaultAsync(s => s.Id == new StocktakeId(command.StocktakeId), cancellationToken)
            ?? throw new DomainException(
                "stocktake_not_found", $"Stocktake {command.StocktakeId} does not exist.");

        var lines = stocktake.Lines.ToList();
        foreach (var accepted in command.Lines)
        {
            if (accepted.LineIndex < 0 || accepted.LineIndex >= lines.Count)
            {
                continue;
            }

            var line = lines[accepted.LineIndex];
            var item = await stockItems.GetAtAsync(line.Sku, line.Batch, line.Location, cancellationToken);
            if (item is null || item.OnHand == line.Counted)
            {
                continue;
            }

            var movement = item.AdjustTo(line.Counted, $"Stocktake: {accepted.Reason}", command.PerformedBy);
            ledger.Append(movement);
            stockItems.Update(item);
        }

        // Closes the stocktake (status → Approved); the recorded differences are now reconciled.
        stocktake.Approve();
        await db.SaveChangesAsync(cancellationToken);
    }
}
