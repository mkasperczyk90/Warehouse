using Warehouse.Warehousing.Inventory.Application.ConfirmPutAway;
using Warehouse.Warehousing.Inventory.Application.ProposePutAway;

namespace Warehouse.Warehousing.Api;

/// <summary>
/// Inventory HTTP surface for the inbound flow's put-away (UC-04). Receiving itself is event-driven
/// (no endpoint — it reacts to Logistics' goods-receipt event), so only put-away is exposed here.
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

        return app;
    }
}
