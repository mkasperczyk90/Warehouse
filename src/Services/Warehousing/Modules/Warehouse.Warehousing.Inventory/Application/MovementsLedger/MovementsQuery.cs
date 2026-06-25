using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Warehouse.Warehousing.Inventory.Domain;
using Warehouse.Warehousing.Inventory.Infrastructure.Persistence;

namespace Warehouse.Warehousing.Inventory.Application.MovementsLedger;

/// <summary>
/// UC-05 read model: the immutable stock-movement ledger (ADR-0002 — stock is its projection). Read-only
/// and append-only by design. Each entry is flattened to the admin's ledger row: a UI category + label, a
/// <b>signed</b> quantity (positive into stock, negative out) and the location the fact happened at. Scoped
/// to one warehouse via the location's first segment (the warehouse code), which the desk sends as
/// <c>X-Warehouse-Id</c>.
/// </summary>
public sealed record MovementRowDto(
    string Id,
    string Date,
    string Type,
    string TypeLabel,
    string Product,
    string Sku,
    string Batch,
    string Location,
    decimal Qty,
    string Unit,
    string Reference);

public sealed class MovementsHandler(InventoryDbContext db)
{
    /// <summary>How many recent entries the ledger view pulls (newest first). Paging is a later concern.</summary>
    private const int MaxRows = 200;

    public async Task<IReadOnlyList<MovementRowDto>> ListAsync(
        string warehouse, CancellationToken cancellationToken = default)
    {
        var products = await db.ProductSnapshots.AsNoTracking().ToListAsync(cancellationToken);
        var nameBySku = products.ToDictionary(p => p.Sku.Value, p => p.Name);

        var movements = await db.StockMovements.AsNoTracking()
            .OrderByDescending(m => m.OccurredAt)
            .Take(MaxRows)
            .ToListAsync(cancellationToken);

        var rows = new List<MovementRowDto>(movements.Count);
        foreach (var m in movements)
        {
            // One physical fact: a transfer/in lands at its target, an out leaves its source.
            var into = m.To is not null;
            var location = (into ? m.To : m.From)!.Value;
            if (WarehouseOf(location) != warehouse)
            {
                continue;
            }

            var (category, label) = Categorize(m.Type);
            rows.Add(new MovementRowDto(
                m.Id.Value.ToString(),
                m.OccurredAt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
                category,
                label,
                nameBySku.GetValueOrDefault(m.Sku.Value, m.Sku.Value),
                m.Sku.Value,
                m.Batch?.Value ?? "—",
                location,
                into ? m.Quantity.Amount : -m.Quantity.Amount,
                m.Quantity.Unit.Code,
                m.Reason ?? "—"));
        }

        return rows;
    }

    /// <summary>Warehouse code is the first segment of a location address ("WH01-CR1-A03-R2-S1").</summary>
    private static string WarehouseOf(string location)
    {
        var dash = location.IndexOf('-', StringComparison.Ordinal);
        return dash < 0 ? location : location[..dash];
    }

    /// <summary>Maps the domain movement type onto the admin's five ledger pills + a human label.</summary>
    private static (string Category, string Label) Categorize(MovementType type) => type switch
    {
        MovementType.GoodsReceipt => ("receipt", "Goods receipt"),
        MovementType.PutAway => ("putaway", "Put-away"),
        MovementType.Pick => ("pick", "Pick"),
        MovementType.Dispatch => ("pick", "Dispatch"),
        MovementType.Move => ("move", "Move"),
        MovementType.TransferIn => ("move", "Transfer in"),
        MovementType.TransferOut => ("move", "Transfer out"),
        MovementType.AdjustmentIn => ("adjustment", "Adjustment"),
        MovementType.AdjustmentOut => ("adjustment", "Adjustment"),
        MovementType.StocktakeDifference => ("adjustment", "Stocktake difference"),
        _ => ("adjustment", type.ToString()),
    };
}
