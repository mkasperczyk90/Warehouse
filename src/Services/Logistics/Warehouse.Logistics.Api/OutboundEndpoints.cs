using Warehouse.Logistics.Core.Application.CancelOrder;
using Warehouse.Logistics.Core.Application.ConfirmDispatch;
using Warehouse.Logistics.Core.Application.ConfirmPick;
using Warehouse.Logistics.Core.Application.CreateOutboundOrder;
using Warehouse.Logistics.Core.Application.GetOrder;
using Warehouse.Logistics.Core.Application.GetPickList;
using Warehouse.Logistics.Core.Application.ListOrders;
using Warehouse.Logistics.Core.Application.MarkPacked;
using Warehouse.Logistics.Core.Application.ReportShortPick;
using Warehouse.Logistics.Core.Application.StartPicking;
using Warehouse.Logistics.Core.Domain;

namespace Warehouse.Logistics.Api;

/// <summary>
/// Outbound order HTTP surface (UC-09…UC-12). Thin: each route maps the request onto a use-case
/// command and delegates to its handler; domain failures are translated by <see cref="DomainExceptionHandler"/>.
/// </summary>
internal static class OutboundEndpoints
{
    public static IEndpointRouteBuilder MapOutboundEndpoints(this IEndpointRouteBuilder app)
    {
        var orders = app.MapGroup("/logistics/orders");

        // UC-09: place an order (soft reservation happens in Inventory, reported back).
        orders.MapPost("/", async (
            CreateOutboundOrderCommand command, CreateOutboundOrderHandler handler, CancellationToken ct) =>
        {
            var id = await handler.HandleAsync(command, ct);
            return Results.Created($"/logistics/orders/{id}", new { id });
        });

        // UC-10: release to picking.
        orders.MapPost("/{id:guid}/picking", async (
            Guid id, StartPickingHandler handler, CancellationToken ct) =>
        {
            await handler.HandleAsync(new StartPickingCommand(id), ct);
            return Results.NoContent();
        });

        // UC-11: packed.
        orders.MapPost("/{id:guid}/packed", async (
            Guid id, MarkPackedHandler handler, CancellationToken ct) =>
        {
            await handler.HandleAsync(new MarkPackedCommand(id), ct);
            return Results.NoContent();
        });

        // UC-12: dispatch to carrier (publishes ShipmentDispatched).
        orders.MapPost("/{id:guid}/dispatch", async (
            Guid id, DispatchRequest request, ConfirmDispatchHandler handler, CancellationToken ct) =>
        {
            await handler.HandleAsync(
                new ConfirmDispatchCommand(id, request.CarrierRoleId, request.TrackingNumber, request.PackageWeightKg), ct);
            return Results.NoContent();
        });

        // Cancel (reservations released in Inventory).
        orders.MapPost("/{id:guid}/cancel", async (
            Guid id, CancelOrderHandler handler, CancellationToken ct) =>
        {
            await handler.HandleAsync(new CancelOrderCommand(id), ct);
            return Results.NoContent();
        });

        orders.MapGet("/", async (
            OrderStatus? status, ListOrdersHandler handler, CancellationToken ct) =>
            Results.Ok(await handler.HandleAsync(new ListOrdersQuery(status), ct)));

        orders.MapGet("/{id:guid}", async (
            Guid id, GetOrderHandler handler, CancellationToken ct) =>
        {
            var dto = await handler.HandleAsync(new GetOrderQuery(id), ct);
            return dto is null ? Results.NotFound() : Results.Ok(dto);
        });

        // UC-10: the order's pick list (terminal worklist).
        orders.MapGet("/{id:guid}/pick-list", async (
            Guid id, GetPickListHandler handler, CancellationToken ct) =>
        {
            var dto = await handler.HandleAsync(new GetPickListQuery(id), ct);
            return dto is null ? Results.NotFound() : Results.Ok(dto);
        });

        // UC-10: confirm a pick task (deducts the allocation in Inventory).
        orders.MapPost("/{id:guid}/picks/{sequence:int}/confirm", async (
            Guid id, int sequence, ConfirmPickRequest? request, ConfirmPickHandler handler, CancellationToken ct) =>
        {
            await handler.HandleAsync(new ConfirmPickCommand(id, sequence, request?.PickedBy ?? "terminal"), ct);
            return Results.NoContent();
        });

        // UC-10 exception: report a short pick.
        orders.MapPost("/{id:guid}/picks/{sequence:int}/short", async (
            Guid id, int sequence, ShortPickRequest? request, ReportShortPickHandler handler, CancellationToken ct) =>
        {
            await handler.HandleAsync(
                new ReportShortPickCommand(id, sequence, request?.ReportedBy ?? "terminal", request?.Reason), ct);
            return Results.NoContent();
        });

        return app;
    }
}

internal sealed record DispatchRequest(Guid CarrierRoleId, string? TrackingNumber, decimal PackageWeightKg);

internal sealed record ConfirmPickRequest(string? PickedBy);

internal sealed record ShortPickRequest(string? ReportedBy, string? Reason);
