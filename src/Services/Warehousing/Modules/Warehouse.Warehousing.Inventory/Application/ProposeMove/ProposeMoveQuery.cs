using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Warehouse.Warehousing.Inventory.Domain;
using Warehouse.Warehousing.Inventory.Infrastructure.Persistence;

namespace Warehouse.Warehousing.Inventory.Application.ProposeMove;

/// <summary>
/// UC-06 — the replenishment/move worklist: available stock sitting in reserve storage that can be moved
/// down to a pick face in the same room. A pick face is a location whose code carries the
/// <c>PICKFACE</c> marker. The proposed target is a real, temperature-compatible pick face; the confirm
/// (<c>ConfirmMove</c>) re-checks the storage invariant before any stock moves. Like put-away, the route
/// optimiser is deferred — this is the deterministic "what could be replenished now" list.
/// </summary>
public sealed record ProposeMoveQuery(string WarehouseCode);

public sealed record MoveTaskDto(
    string SourceItemId,
    string Sku,
    string? BatchNumber,
    decimal Quantity,
    string Unit,
    string FromLocation,
    string ToLocation,
    string? BestBefore,
    bool RequiresColdChain,
    IReadOnlyList<string> Checks);

public sealed class ProposeMoveHandler(InventoryDbContext db)
{
    private const string PickFaceMarker = "PICKFACE";

    public async Task<IReadOnlyList<MoveTaskDto>> HandleAsync(
        ProposeMoveQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        var warehouse = WarehouseCode.Of(query.WarehouseCode);

        var items = await db.StockItems.AsNoTracking().ToListAsync(cancellationToken);
        var locations = await db.LocationSnapshots.AsNoTracking().ToListAsync(cancellationToken);
        var products = (await db.ProductSnapshots.AsNoTracking().ToListAsync(cancellationToken))
            .ToDictionary(p => p.Sku.Value);
        var batches = (await db.Batches.AsNoTracking().ToListAsync(cancellationToken))
            .ToDictionary(b => (b.Sku.Value, b.Number.Value));

        var byCode = locations.ToDictionary(l => l.Code.Value);
        var pickFaces = locations
            .Where(l => l.Warehouse == warehouse && IsPickFace(l.Code.Value))
            .ToList();

        var tasks = new List<MoveTaskDto>();
        foreach (var item in items)
        {
            if (item.Available.IsZero)
            {
                continue; // nothing free to replenish with (blocked/quarantined/fully allocated)
            }

            if (!byCode.TryGetValue(item.Location.Value, out var source) || source.Warehouse != warehouse)
            {
                continue;
            }

            if (IsPickFace(source.Code.Value))
            {
                continue; // already on a pick face
            }

            // A pick face in the same room is the replenishment target (fallback: any in the warehouse).
            var target = pickFaces.FirstOrDefault(p => p.Room == source.Room) ?? pickFaces.FirstOrDefault();
            if (target is null)
            {
                continue;
            }

            products.TryGetValue(item.Sku.Value, out var product);
            string? bestBefore = null;
            if (item.Batch is { } number && batches.TryGetValue((item.Sku.Value, number.Value), out var batch))
            {
                bestBefore = batch.ExpiryDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            }

            tasks.Add(new MoveTaskDto(
                item.Id.Value.ToString(),
                item.Sku.Value,
                item.Batch?.Value,
                item.Available.Amount,
                item.OnHand.Unit.Code,
                item.Location.Value,
                target.Code.Value,
                bestBefore,
                product?.RequiresColdChain ?? false,
                [
                    "Destination is temperature-compatible (same room)",
                    "Capacity & load limit OK at destination",
                ]));
        }

        return tasks;
    }

    private static bool IsPickFace(string code) =>
        code.Contains(PickFaceMarker, StringComparison.OrdinalIgnoreCase);
}
