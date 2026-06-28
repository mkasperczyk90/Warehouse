using Warehouse.Warehousing.Inventory.Application.ConfirmMove;
using Warehouse.Warehousing.Inventory.Application.ConfirmPutAway;
using Warehouse.Warehousing.Inventory.Application.ProposeMove;
using Warehouse.Warehousing.Inventory.Application.ProposePutAway;

namespace Warehouse.Warehousing.Api;

/// <summary>
/// Inventory HTTP surface for the terminal's put-away (UC-04) and replenishment moves (UC-06). Receiving
/// itself is event-driven (no endpoint — it reacts to Logistics' goods-receipt event).
/// </summary>
internal static class InventoryEndpoints
{
    public static IEndpointRouteBuilder MapInventoryEndpoints(this IEndpointRouteBuilder app)
    {
        var putAway = app.MapGroup("/inventory/put-away");

        // The put-away worklist for a warehouse (dock-buffer contents).
        putAway.MapGet("/tasks", async (
            string warehouse, ProposePutAwayHandler handler, CancellationToken ct) =>
            Results.Ok(await handler.HandleAsync(new ProposePutAwayQuery(warehouse), ct)));

        // Confirm a scanned put-away (buffer → storage location).
        putAway.MapPost("/confirm", async (
            ConfirmPutAwayCommand command, ConfirmPutAwayHandler handler, CancellationToken ct) =>
        {
            await handler.HandleAsync(command, ct);
            return Results.NoContent();
        });

        var moves = app.MapGroup("/inventory/moves");

        // The replenishment/move worklist for a warehouse (reserve → pick face).
        moves.MapGet("/", async (
            string warehouse, ProposeMoveHandler handler, CancellationToken ct) =>
            Results.Ok(await handler.HandleAsync(new ProposeMoveQuery(warehouse), ct)));

        // Confirm a scanned replenishment move (source item → pick face).
        moves.MapPost("/confirm", async (
            ConfirmMoveCommand command, ConfirmMoveHandler handler, CancellationToken ct) =>
        {
            await handler.HandleAsync(command, ct);
            return Results.NoContent();
        });

        return app;
    }
}
