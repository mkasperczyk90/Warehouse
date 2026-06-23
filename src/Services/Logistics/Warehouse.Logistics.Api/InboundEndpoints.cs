using Warehouse.Logistics.Core.Application.Deliveries.AnnounceDelivery;
using Warehouse.Logistics.Core.Application.Deliveries.AssignDockSlot;
using Warehouse.Logistics.Core.Application.Deliveries.CancelDelivery;
using Warehouse.Logistics.Core.Application.Deliveries.ConfirmReceipt;
using Warehouse.Logistics.Core.Application.Deliveries.GetDelivery;
using Warehouse.Logistics.Core.Application.Deliveries.ListDeliveries;
using Warehouse.Logistics.Core.Application.Deliveries.RecordReceiptLine;
using Warehouse.Logistics.Core.Application.Deliveries.RegisterArrival;
using Warehouse.Logistics.Core.Application.Deliveries.StartReceiving;
using Warehouse.Logistics.Core.Domain;

namespace Warehouse.Logistics.Api;

/// <summary>
/// Inbound delivery HTTP surface (UC-01…UC-02 plus the put-away lifecycle transitions). Thin: each
/// route maps the request onto a use-case command and delegates to its handler. Domain failures are
/// translated centrally by <see cref="DomainExceptionHandler"/>.
/// </summary>
internal static class InboundEndpoints
{
    public static IEndpointRouteBuilder MapInboundEndpoints(this IEndpointRouteBuilder app)
    {
        var deliveries = app.MapGroup("/logistics/deliveries");

        // UC-01: announce an ASN.
        deliveries.MapPost("/", async (
            AnnounceDeliveryCommand command, AnnounceDeliveryHandler handler, CancellationToken ct) =>
        {
            var id = await handler.HandleAsync(command, ct);
            return Results.Created($"/logistics/deliveries/{id}", new { id });
        });

        // UC-01: book a dock slot.
        deliveries.MapPost("/{id:guid}/dock-slot", async (
            Guid id, AssignDockSlotRequest request, AssignDockSlotHandler handler, CancellationToken ct) =>
        {
            await handler.HandleAsync(new AssignDockSlotCommand(id, request.DockCode, request.From, request.To), ct);
            return Results.NoContent();
        });

        // UC-02: truck arrives.
        deliveries.MapPost("/{id:guid}/arrival", async (
            Guid id, RegisterArrivalHandler handler, CancellationToken ct) =>
        {
            await handler.HandleAsync(new RegisterArrivalCommand(id), ct);
            return Results.NoContent();
        });

        // UC-02: scanning starts.
        deliveries.MapPost("/{id:guid}/receiving", async (
            Guid id, StartReceivingHandler handler, CancellationToken ct) =>
        {
            await handler.HandleAsync(new StartReceivingCommand(id), ct);
            return Results.NoContent();
        });

        // UC-02: record a counted line.
        deliveries.MapPost("/{id:guid}/lines/{lineNo:int}/receipt", async (
            Guid id, int lineNo, RecordReceiptRequest request, RecordReceiptLineHandler handler, CancellationToken ct) =>
        {
            await handler.HandleAsync(
                new RecordReceiptLineCommand(
                    id, lineNo, request.Quantity, request.Unit,
                    request.BatchNumber, request.ExpiryDate, request.Discrepancy ?? "None", request.Note),
                ct);
            return Results.NoContent();
        });

        // UC-02: confirm the whole receipt (publishes GoodsReceiptConfirmed).
        deliveries.MapPost("/{id:guid}/confirm", async (
            Guid id, ConfirmReceiptHandler handler, CancellationToken ct) =>
        {
            await handler.HandleAsync(new ConfirmReceiptCommand(id), ct);
            return Results.NoContent();
        });

        // Cancel an announced delivery.
        deliveries.MapPost("/{id:guid}/cancel", async (
            Guid id, CancelDeliveryHandler handler, CancellationToken ct) =>
        {
            await handler.HandleAsync(new CancelDeliveryCommand(id), ct);
            return Results.NoContent();
        });

        // List (optionally by status).
        deliveries.MapGet("/", async (
            DeliveryStatus? status, ListDeliveriesHandler handler, CancellationToken ct) =>
            Results.Ok(await handler.HandleAsync(new ListDeliveriesQuery(status), ct)));

        // Read one.
        deliveries.MapGet("/{id:guid}", async (
            Guid id, GetDeliveryHandler handler, CancellationToken ct) =>
        {
            var dto = await handler.HandleAsync(new GetDeliveryQuery(id), ct);
            return dto is null ? Results.NotFound() : Results.Ok(dto);
        });

        return app;
    }
}

internal sealed record AssignDockSlotRequest(string DockCode, DateTimeOffset From, DateTimeOffset To);

internal sealed record RecordReceiptRequest(
    decimal Quantity,
    string Unit,
    string? BatchNumber,
    DateOnly? ExpiryDate,
    string? Discrepancy,
    string? Note);
