using Warehouse.Warehousing.Inventory.Application.Stocktakes;

namespace Warehouse.Warehousing.Api;

/// <summary>
/// Stocktake HTTP surface for the admin (UC-07): list / review counts, start a blind count, and approve a
/// counted one into ledger adjustments. Warehouse-scoped via <c>X-Warehouse-Id</c>. Backs the admin's
/// <c>inventory/stocktake*</c> calls (ADR-0006).
/// </summary>
internal static class StocktakeEndpoints
{
    private const string WarehouseHeader = "X-Warehouse-Id";
    private const string DefaultWarehouse = "WH01";

    public static IEndpointRouteBuilder MapStocktakeEndpoints(this IEndpointRouteBuilder app)
    {
        var stocktake = app.MapGroup("/inventory/stocktake");

        stocktake.MapGet("/", async (HttpRequest request, StocktakeQueries queries, CancellationToken ct) =>
            Results.Ok(await queries.ListAsync(Warehouse(request), ct)));

        stocktake.MapGet("/{id}", async (string id, StocktakeQueries queries, CancellationToken ct) =>
        {
            var detail = await queries.DetailAsync(id, ct);
            return detail is null ? Results.NotFound() : Results.Ok(detail);
        });

        stocktake.MapPost("/", async (
            HttpRequest request, StartStocktakeRequest body, StartStocktakeHandler handler, CancellationToken ct) =>
        {
            var id = await handler.HandleAsync(new StartStocktakeCommand(Warehouse(request), body.Scope, "Desk"), ct);
            return Results.Ok(new { id = id.ToString() });
        });

        stocktake.MapPost("/{id}/approve", async (
            string id, ApproveStocktakeRequest body, ApproveStocktakeHandler handler, CancellationToken ct) =>
        {
            var lines = (body.Rows ?? [])
                .Select(r => new ApproveLine(int.Parse(r.Id, System.Globalization.CultureInfo.InvariantCulture), r.Reason))
                .ToList();
            await handler.HandleAsync(new ApproveStocktakeCommand(Guid.Parse(id), lines, "Desk"), ct);
            return Results.Ok(new { posted = true });
        });

        // Recount is a terminal concern (re-issue a blind count to operators); the desk action is a no-op
        // acknowledgement until the counting workflow lands.
        stocktake.MapPost("/{id}/recount", (string id) => Results.NoContent());

        return app;
    }

    private static string Warehouse(HttpRequest request) =>
        request.Headers.TryGetValue(WarehouseHeader, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value.ToString()
            : DefaultWarehouse;
}

internal sealed record StartStocktakeRequest(string Scope);

internal sealed record ApproveRow(string Id, string Reason);

internal sealed record ApproveStocktakeRequest(IReadOnlyList<ApproveRow>? Rows);
