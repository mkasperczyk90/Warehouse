using Warehouse.Warehousing.Inventory.Application.Abstractions;
using Warehouse.Warehousing.Inventory.Domain;

namespace Warehouse.Warehousing.Inventory.Application.ProposePutAway;

/// <summary>
/// UC-04 — the put-away worklist: what is sitting in a warehouse's dock buffer waiting to be stored.
/// The target-location optimiser is deliberately deferred (docs/PLAN.md), so a task carries no
/// suggested location yet — the operator scans the destination on confirm.
/// </summary>
public sealed record ProposePutAwayQuery(string WarehouseCode);

public sealed record PutAwayTaskDto(
    string Sku,
    string? BatchNumber,
    decimal Quantity,
    string Unit,
    string FromLocation);

public sealed class ProposePutAwayHandler(IStockItemRepository stockItems)
{
    public async Task<IReadOnlyList<PutAwayTaskDto>> HandleAsync(
        ProposePutAwayQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var buffer = DockBuffer.For(WarehouseCode.Of(query.WarehouseCode));
        var items = await stockItems.ListAtAsync(buffer, cancellationToken);

        return items
            .Where(i => !i.OnHand.IsZero)
            .Select(i => new PutAwayTaskDto(
                i.Sku.Value,
                i.Batch?.Value,
                i.OnHand.Amount,
                i.OnHand.Unit.Code,
                i.Location.Value))
            .ToList();
    }
}
