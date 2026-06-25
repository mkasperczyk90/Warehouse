using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Warehouse.Warehousing.Inventory.Domain;
using Warehouse.Warehousing.Inventory.Infrastructure.Persistence;

namespace Warehouse.Warehousing.Inventory.Application.StockOverview;

/// <summary>
/// UC-05 read model: the desk's stock view. A projection over the Inventory aggregates
/// (<see cref="StockItem"/> + <see cref="Batch"/> + the local Product/Location replicas), shaped to the
/// admin panel's contract (one on-hand row, with a derived domain status). Read-only and scoped to one
/// warehouse — the gateway forwards the desk's <c>X-Warehouse-Id</c>, resolved here against each item's
/// location replica. The dataset is small, so we materialize the aggregates and join in memory (their
/// value-converted columns don't translate to SQL joins).
/// </summary>
public sealed record StockRowDto(
    string Id,
    string Product,
    string Sku,
    string Batch,
    string BestBefore,
    string Location,
    string Room,
    decimal OnHand,
    decimal Atp,
    string Unit,
    string Status,
    string StatusLabel);

public sealed record StockKpisDto(
    decimal OnHand,
    decimal Atp,
    decimal Reserved,
    decimal BlockedExpiring,
    string Unit);

public sealed record StockMovementRowDto(string Id, string Date, string Type, decimal Qty, string Reference);

public sealed record StockItemDetailDto(
    string Id,
    string Product,
    string Sku,
    string Batch,
    string BestBefore,
    string Location,
    string Room,
    decimal OnHand,
    decimal Atp,
    decimal Reserved,
    string Unit,
    string Status,
    string StatusLabel,
    IReadOnlyList<StockMovementRowDto> Movements);

public sealed record StockBySkuRowDto(
    string Location,
    string Room,
    decimal OnHand,
    decimal Atp,
    string Status,
    string StatusLabel);

public sealed record MoveLocationDto(string Address, string Room, string RoomType);

/// <summary>The item being corrected on the Adjustment screen (UC-08) — its identity + system on-hand.</summary>
public sealed record AdjustmentDraftDto(
    string ItemId,
    string Product,
    string Sku,
    string Batch,
    string Location,
    string Room,
    string Status,
    string StatusLabel,
    decimal SystemOnHand,
    string Unit);

public sealed class StockOverviewHandler(InventoryDbContext db)
{
    /// <summary>Expiry window (days) before a batch shows as "expiring" in the stock view.</summary>
    private const int ExpiringWithinDays = 7;

    public async Task<IReadOnlyList<StockRowDto>> ListRowsAsync(
        string warehouse, CancellationToken cancellationToken = default)
    {
        var ctx = await LoadAsync(warehouse, cancellationToken);
        return ctx.ScopedItems.Select(ctx.ToRow).ToList();
    }

    public async Task<StockKpisDto> KpisAsync(string warehouse, CancellationToken cancellationToken = default)
    {
        var ctx = await LoadAsync(warehouse, cancellationToken);
        var rows = ctx.ScopedItems.Select(ctx.ToRow).ToList();
        return new StockKpisDto(
            OnHand: rows.Sum(r => r.OnHand),
            Atp: rows.Sum(r => r.Atp),
            Reserved: rows.Sum(r => r.OnHand - r.Atp),
            BlockedExpiring: rows.Where(r => r.Status is "blocked" or "expired").Sum(r => r.OnHand),
            Unit: "units");
    }

    public async Task<StockItemDetailDto?> ItemAsync(
        string id, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(id, out var guid))
        {
            return null;
        }

        var item = await db.StockItems.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == new StockItemId(guid), cancellationToken);
        if (item is null)
        {
            return null;
        }

        var ctx = await LoadAsync(warehouse: null, cancellationToken);
        var row = ctx.ToRow(item);

        var movements = await db.StockMovements.AsNoTracking()
            .Where(m => m.Sku == item.Sku)
            .OrderByDescending(m => m.OccurredAt)
            .ToListAsync(cancellationToken);

        var history = movements
            .Where(m => m.From == item.Location || m.To == item.Location)
            .Where(m => item.Batch is null ? m.Batch is null : m.Batch == item.Batch)
            .Select(m => new StockMovementRowDto(
                m.Id.Value.ToString(),
                m.OccurredAt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
                m.Type.ToString(),
                // Signed for the ledger view: leaving the location is negative.
                m.From == item.Location ? -m.Quantity.Amount : m.Quantity.Amount,
                m.Reason ?? "—"))
            .ToList();

        return new StockItemDetailDto(
            row.Id, row.Product, row.Sku, row.Batch, row.BestBefore, row.Location, row.Room,
            row.OnHand, row.Atp, row.OnHand - row.Atp, row.Unit, row.Status, row.StatusLabel, history);
    }

    public async Task<IReadOnlyList<StockBySkuRowDto>> BySkuAsync(
        string sku, string warehouse, CancellationToken cancellationToken = default)
    {
        var normalized = Sku.Of(sku);
        var ctx = await LoadAsync(warehouse, cancellationToken);
        return ctx.ScopedItems
            .Where(i => i.Sku == normalized)
            .Select(i =>
            {
                var row = ctx.ToRow(i);
                return new StockBySkuRowDto(row.Location, row.Room, row.OnHand, row.Atp, row.Status, row.StatusLabel);
            })
            .ToList();
    }

    /// <summary>Adjustment draft for a specific stock item (reached from a Stock row).</summary>
    public async Task<AdjustmentDraftDto?> AdjustmentDraftAsync(string id, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(id, out var guid))
        {
            return null;
        }

        var item = await db.StockItems.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == new StockItemId(guid), cancellationToken);
        if (item is null)
        {
            return null;
        }

        var ctx = await LoadAsync(warehouse: null, cancellationToken);
        return ToDraft(ctx.ToRow(item));
    }

    /// <summary>Default draft (no item chosen yet): the warehouse's first stock line, until the
    /// ad-hoc SKU/location picker exists. Lets the bare <c>/adjustment</c> route render something.</summary>
    public async Task<AdjustmentDraftDto?> DefaultAdjustmentDraftAsync(
        string warehouse, CancellationToken cancellationToken = default)
    {
        var ctx = await LoadAsync(warehouse, cancellationToken);
        var first = ctx.ScopedItems.FirstOrDefault();
        return first is null ? null : ToDraft(ctx.ToRow(first));
    }

    private static AdjustmentDraftDto ToDraft(StockRowDto row) => new(
        row.Id, row.Product, row.Sku, row.Batch, row.Location, row.Room,
        row.Status, row.StatusLabel, row.OnHand, row.Unit);

    public async Task<IReadOnlyList<MoveLocationDto>> LocationsAsync(
        string warehouse, CancellationToken cancellationToken = default)
    {
        var code = Domain.WarehouseCode.Of(warehouse);
        var snapshots = await db.LocationSnapshots.AsNoTracking().ToListAsync(cancellationToken);
        return snapshots
            .Where(s => s.Warehouse == code)
            .Select(s => new MoveLocationDto(s.Code.Value, s.Room, RoomTypeOf(s)))
            .ToList();
    }

    private async Task<Projection> LoadAsync(string? warehouse, CancellationToken cancellationToken)
    {
        var items = await db.StockItems.AsNoTracking().ToListAsync(cancellationToken);
        var locations = await db.LocationSnapshots.AsNoTracking().ToListAsync(cancellationToken);
        var products = await db.ProductSnapshots.AsNoTracking().ToListAsync(cancellationToken);
        var batches = await db.Batches.AsNoTracking().ToListAsync(cancellationToken);

        return new Projection(
            items,
            locations.ToDictionary(l => l.Code.Value),
            products.ToDictionary(p => p.Sku.Value),
            batches.ToDictionary(b => (b.Sku.Value, b.Number.Value)),
            warehouse is null ? null : Domain.WarehouseCode.Of(warehouse));
    }

    private static string RoomTypeOf(Domain.Replicas.LocationSnapshot s)
    {
        if (s.IsHazmatZone)
        {
            return "hazmat";
        }

        var max = s.EnvironmentTemperature.MaxCelsius;
        return max <= -10m ? "freezer" : max <= 8m ? "cold" : "standard";
    }

    /// <summary>The materialized aggregates plus the lookups needed to shape a row, scoped to a warehouse.</summary>
    private sealed class Projection(
        IReadOnlyList<StockItem> items,
        Dictionary<string, Domain.Replicas.LocationSnapshot> locationsByCode,
        Dictionary<string, Domain.Replicas.ProductSnapshot> productsBySku,
        Dictionary<(string Sku, string Batch), Batch> batchesByKey,
        Domain.WarehouseCode? warehouse)
    {
        public IEnumerable<StockItem> ScopedItems => (warehouse is null
                ? items
                : items.Where(i =>
                    locationsByCode.TryGetValue(i.Location.Value, out var loc) && loc.Warehouse == warehouse))
            .Where(i => !i.OnHand.IsZero);

        public StockRowDto ToRow(StockItem item)
        {
            locationsByCode.TryGetValue(item.Location.Value, out var loc);
            productsBySku.TryGetValue(item.Sku.Value, out var product);
            Batch? batch = null;
            if (item.Batch is { } number)
            {
                batchesByKey.TryGetValue((item.Sku.Value, number.Value), out batch);
            }

            var (status, label) = DeriveStatus(item, batch);
            return new StockRowDto(
                item.Id.Value.ToString(),
                product?.Name ?? item.Sku.Value,
                item.Sku.Value,
                item.Batch?.Value ?? "—",
                batch?.ExpiryDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "—",
                item.Location.Value,
                loc?.Room ?? "—",
                item.OnHand.Amount,
                item.Available.Amount,
                item.OnHand.Unit.Code,
                status,
                label);
        }
    }

    /// <summary>Maps the domain status (+ batch expiry) onto the front's StatusVariant + a human label.</summary>
    private static (string Status, string Label) DeriveStatus(StockItem item, Batch? batch)
    {
        if (item.Status == StockStatus.Blocked)
        {
            return ("blocked", "Blocked · QC");
        }

        if (item.Status == StockStatus.Quarantine)
        {
            return ("blocked", "Quarantine");
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (batch?.ExpiryDate is { } expiry)
        {
            if (expiry < today)
            {
                return ("expired", "Expired");
            }

            var days = expiry.DayNumber - today.DayNumber;
            if (days <= ExpiringWithinDays)
            {
                return ("expired", $"Expiring {days}d");
            }
        }

        return item.Allocated.Amount > 0
            ? ("reserved", "Reserved")
            : ("available", "Available");
    }
}
