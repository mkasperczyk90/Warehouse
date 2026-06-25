using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Warehouse.Warehousing.Inventory.Domain;
using Warehouse.Warehousing.Inventory.Infrastructure.Persistence;

namespace Warehouse.Warehousing.Inventory.Application.Stocktakes;

/// <summary>
/// UC-07 read model: the stocktake list + review detail for the admin. A stocktake is a blind count over a
/// set of locations; the desk reviews the differences and approves them into ledger adjustments. State is
/// derived from the aggregate's status + its count lines and mapped to the admin's four UI states; rows are
/// scoped to one warehouse via the first segment (warehouse code) of the counted locations.
/// </summary>
public sealed record StocktakeListItemDto(
    string Id,
    string Scope,
    string State,
    string When,
    int LocationsCounted,
    int TotalLocations,
    int Discrepancies);

public sealed record StocktakeSummaryDto(
    string Id,
    string Title,
    string Sub,
    string State,
    int LocationsCounted,
    int TotalLocations,
    int Matches,
    int Discrepancies,
    decimal NetVariance);

public sealed record StocktakeDiffDto(
    string Id,
    string Location,
    string Product,
    string Batch,
    decimal System,
    decimal Counted,
    decimal Delta,
    string? DefaultReason);

public sealed record StocktakeDetailDto(StocktakeSummaryDto Summary, IReadOnlyList<StocktakeDiffDto> Diffs);

public sealed class StocktakeQueries(InventoryDbContext db)
{
    public async Task<IReadOnlyList<StocktakeListItemDto>> ListAsync(
        string warehouse, CancellationToken cancellationToken = default)
    {
        var stocktakes = await db.Stocktakes.AsNoTracking()
            .OrderByDescending(s => s.StartedAt)
            .ToListAsync(cancellationToken);

        return stocktakes
            .Where(s => WarehouseOf(s) == warehouse)
            .Select(s =>
            {
                var view = View.Of(s);
                return new StocktakeListItemDto(
                    s.Id.Value.ToString(), s.Label, view.State, view.When,
                    view.LocationsCounted, s.Scope.Count, view.Discrepancies);
            })
            .ToList();
    }

    public async Task<StocktakeDetailDto?> DetailAsync(string id, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(id, out var guid))
        {
            return null;
        }

        var stocktake = await db.Stocktakes.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == new StocktakeId(guid), cancellationToken);
        if (stocktake is null)
        {
            return null;
        }

        var products = await db.ProductSnapshots.AsNoTracking().ToListAsync(cancellationToken);
        var nameBySku = products.ToDictionary(p => p.Sku.Value, p => p.Name);

        var view = View.Of(stocktake);
        var summary = new StocktakeSummaryDto(
            stocktake.Id.Value.ToString(),
            $"Stocktake — {stocktake.Label}",
            view.When,
            view.State,
            view.LocationsCounted,
            stocktake.Scope.Count,
            view.Matches,
            view.Discrepancies,
            view.NetVariance);

        var lines = stocktake.Lines.ToList();
        var diffs = lines
            .Select((line, index) => (line, index))
            .Where(x => x.line.HasDifference)
            .Select(x => new StocktakeDiffDto(
                x.index.ToString(CultureInfo.InvariantCulture),
                x.line.Location.Value,
                nameBySku.GetValueOrDefault(x.line.Sku.Value, x.line.Sku.Value),
                x.line.Batch?.Value ?? "—",
                x.line.Expected.Amount,
                x.line.Counted.Amount,
                x.line.Counted.Amount - x.line.Expected.Amount,
                DefaultReason: null))
            .ToList();

        return new StocktakeDetailDto(summary, diffs);
    }

    private static string WarehouseOf(Stocktake stocktake)
    {
        var first = stocktake.Scope.FirstOrDefault()?.Value ?? string.Empty;
        var dash = first.IndexOf('-', StringComparison.Ordinal);
        return dash < 0 ? first : first[..dash];
    }

    /// <summary>The derived view of a stocktake: its UI state, counts and a human "when" line.</summary>
    private readonly record struct View(
        string State, string When, int LocationsCounted, int Matches, int Discrepancies, decimal NetVariance)
    {
        public static View Of(Stocktake s)
        {
            var lines = s.Lines;
            var counted = lines.Select(l => l.Location.Value).Distinct().Count();
            var discrepancies = lines.Count(l => l.HasDifference);
            var matches = lines.Count - discrepancies;
            var netVariance = lines.Sum(l => l.Counted.Amount - l.Expected.Amount);

            var (state, when) = s.Status switch
            {
                StocktakeStatus.Approved => (
                    "completed",
                    $"Approved {s.ApprovedAt?.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)} · posted to ledger"),
                StocktakeStatus.Cancelled => ("completed", "Cancelled"),
                _ when discrepancies > 0 => ("review", $"Counted {counted} locations · awaiting approval"),
                _ when lines.Count > 0 => ("counting", "Blind count in progress"),
                _ => ("counting", $"Scheduled · {s.Scope.Count} locations"),
            };

            return new View(state, when, counted, matches, discrepancies, netVariance);
        }
    }
}
