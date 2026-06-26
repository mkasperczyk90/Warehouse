using Warehouse.Logistics.Core.Application.Dispatch.AdvanceShipment;
using Warehouse.Logistics.Core.Application.Dispatch.AssignCarrier;
using Warehouse.Logistics.Core.Application.Dispatch.GetDispatchBoard;

namespace Warehouse.Logistics.Api;

/// <summary>
/// Dispatch board HTTP surface (UC-12). The board is keyed by shipment id, not order id, since a shipment
/// walks its own carrier-assignment lifecycle. Domain failures are translated by <see cref="DomainExceptionHandler"/>.
/// </summary>
internal static class DispatchEndpoints
{
    public static IEndpointRouteBuilder MapDispatchEndpoints(this IEndpointRouteBuilder app)
    {
        var dispatch = app.MapGroup("/dispatch");

        // The Kanban board: shipments grouped into lifecycle columns.
        dispatch.MapGet("/board", async (GetDispatchBoardHandler handler, CancellationToken ct) =>
            Results.Ok(await handler.HandleAsync(ct)));

        // Book a carrier + pickup slot (AwaitingCarrier → CarrierAssigned).
        dispatch.MapPost("/{id:guid}/assign", async (
            Guid id, AssignCarrierRequest request, AssignCarrierHandler handler, CancellationToken ct) =>
        {
            await handler.HandleAsync(new AssignCarrierCommand(id, request.CarrierCode, request.Pickup), ct);
            return Results.NoContent();
        });

        // Advance one column (send pickup notice, or dispatch on collection).
        dispatch.MapPost("/{id:guid}/advance", async (
            Guid id, AdvanceShipmentHandler handler, CancellationToken ct) =>
        {
            await handler.HandleAsync(new AdvanceShipmentCommand(id), ct);
            return Results.NoContent();
        });

        return app;
    }
}

internal sealed record AssignCarrierRequest(string CarrierCode, string Pickup);
