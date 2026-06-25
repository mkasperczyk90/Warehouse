using Warehouse.Warehousing.Inventory.Application.AdjustStock;
using Warehouse.Warehousing.Inventory.Application.AdjustStockStatus;
using Warehouse.Warehousing.Inventory.Application.MovementsLedger;
using Warehouse.Warehousing.Inventory.Application.StockOverview;

namespace Warehouse.Warehousing.Api;

/// <summary>
/// Inventory's read surface for the admin Stock view (UC-05) plus the desk's row actions. Everything is
/// scoped to the warehouse the desk has selected, sent by the gateway as <c>X-Warehouse-Id</c>. These
/// back the admin panel's <c>inventory/stock/*</c> and <c>inventory/locations</c> calls (ADR-0006).
/// </summary>
internal static class StockEndpoints
{
    private const string WarehouseHeader = "X-Warehouse-Id";
    private const string DefaultWarehouse = "WH01";

    public static IEndpointRouteBuilder MapStockEndpoints(this IEndpointRouteBuilder app)
    {
        var stock = app.MapGroup("/inventory/stock");

        stock.MapGet("/rows", async (HttpRequest request, StockOverviewHandler handler, CancellationToken ct) =>
            Results.Ok(await handler.ListRowsAsync(Warehouse(request), ct)));

        stock.MapGet("/kpis", async (HttpRequest request, StockOverviewHandler handler, CancellationToken ct) =>
            Results.Ok(await handler.KpisAsync(Warehouse(request), ct)));

        stock.MapGet("/item/{id}", async (string id, StockOverviewHandler handler, CancellationToken ct) =>
        {
            var dto = await handler.ItemAsync(id, ct);
            return dto is null ? Results.NotFound() : Results.Ok(dto);
        });

        stock.MapGet("/by-sku/{sku}", async (
            string sku, HttpRequest request, StockOverviewHandler handler, CancellationToken ct) =>
            Results.Ok(await handler.BySkuAsync(sku, Warehouse(request), ct)));

        stock.MapPost("/item/{id}/move", async (
            string id, MoveStockRequest body, MoveStockHandler handler, CancellationToken ct) =>
        {
            await handler.HandleAsync(new MoveStockCommand(Guid.Parse(id), body.ToLocation, "Desk"), ct);
            return Results.NoContent();
        });

        stock.MapPost("/item/{id}/block", async (
            string id, BlockStockRequest body, BlockStockHandler handler, CancellationToken ct) =>
        {
            await handler.HandleAsync(new BlockStockCommand(Guid.Parse(id), body.Reason), ct);
            return Results.NoContent();
        });

        // Move-target picker (admin Stock + Outbound): the warehouse's known storage locations.
        app.MapGet("/inventory/locations", async (
            HttpRequest request, StockOverviewHandler handler, CancellationToken ct) =>
            Results.Ok(await handler.LocationsAsync(Warehouse(request), ct)));

        // The immutable movement ledger (admin Movements view).
        app.MapGet("/inventory/movements", async (
            HttpRequest request, MovementsHandler handler, CancellationToken ct) =>
            Results.Ok(await handler.ListAsync(Warehouse(request), ct)));

        // --- Stock adjustments (UC-08) ---
        var adjustments = app.MapGroup("/inventory/adjustments");

        adjustments.MapGet("/draft", async (
            HttpRequest request, StockOverviewHandler handler, CancellationToken ct) =>
        {
            var draft = await handler.DefaultAdjustmentDraftAsync(Warehouse(request), ct);
            return draft is null ? Results.NotFound() : Results.Ok(draft);
        });

        adjustments.MapGet("/draft/{id}", async (
            string id, StockOverviewHandler handler, CancellationToken ct) =>
        {
            var draft = await handler.AdjustmentDraftAsync(id, ct);
            return draft is null ? Results.NotFound() : Results.Ok(draft);
        });

        adjustments.MapPost("/", async (AdjustStockRequest body, AdjustStockHandler handler, CancellationToken ct) =>
            Results.Ok(await handler.HandleAsync(
                new AdjustStockCommand(Guid.Parse(body.ItemId), body.NewQuantity, body.Reason, body.Note, "Desk"), ct)));

        return app;
    }

    private static string Warehouse(HttpRequest request) =>
        request.Headers.TryGetValue(WarehouseHeader, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value.ToString()
            : DefaultWarehouse;
}

internal sealed record MoveStockRequest(string ToLocation);

internal sealed record BlockStockRequest(string Reason, string? Note);

internal sealed record AdjustStockRequest(string ItemId, decimal NewQuantity, string Reason, string? Note);
