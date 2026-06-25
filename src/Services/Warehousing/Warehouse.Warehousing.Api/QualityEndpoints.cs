using Warehouse.Warehousing.Inventory.Application.Quality;

namespace Warehouse.Warehousing.Api;

/// <summary>
/// QC HTTP surface for the admin (UC-03): the quarantine worklist and the inspector's release/reject
/// decision. Warehouse-scoped via <c>X-Warehouse-Id</c>. Backs the admin's <c>inventory/qc*</c> calls.
/// </summary>
internal static class QualityEndpoints
{
    private const string WarehouseHeader = "X-Warehouse-Id";
    private const string DefaultWarehouse = "WH01";

    public static IEndpointRouteBuilder MapQualityEndpoints(this IEndpointRouteBuilder app)
    {
        var qc = app.MapGroup("/inventory/qc");

        qc.MapGet("/batches", async (HttpRequest request, QcQueries queries, CancellationToken ct) =>
            Results.Ok(await queries.ListAsync(Warehouse(request), ct)));

        qc.MapPost("/{id}/{decision}", async (
            string id, string decision, QcDecisionRequest body, QcDecisionHandler handler, CancellationToken ct) =>
        {
            await handler.HandleAsync(
                new QcDecisionCommand(Guid.Parse(id), decision, body.Reason, body.Note), ct);
            return Results.NoContent();
        });

        return app;
    }

    private static string Warehouse(HttpRequest request) =>
        request.Headers.TryGetValue(WarehouseHeader, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value.ToString()
            : DefaultWarehouse;
}

internal sealed record QcDecisionRequest(string Reason, string? Note);
