using Warehouse.MasterData.Catalog.Application.Products.ChangeProductStorage;
using Warehouse.MasterData.Catalog.Application.Products.DefineProduct;
using Warehouse.MasterData.Catalog.Application.Products.GetProduct;
using Warehouse.MasterData.Catalog.Application.Products.ImportProducts;
using Warehouse.MasterData.Catalog.Application.Products.ListProducts;
using Warehouse.MasterData.Catalog.Application.Products.RenameProduct;
using Warehouse.MasterData.Catalog.Domain;

namespace Warehouse.MasterData.Api;

/// <summary>
/// Product catalog HTTP surface. Thin: each route maps the request onto a use-case command/query and
/// delegates to its handler. Domain failures are translated centrally by <see cref="DomainExceptionHandler"/>.
/// </summary>
internal static class CatalogEndpoints
{
    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        var products = app.MapGroup("/catalog/products");

        // Define a product and announce it through the transactional outbox.
        products.MapPost("/", async (
            DefineProductCommand command, DefineProductHandler handler, CancellationToken ct) =>
        {
            var sku = await handler.HandleAsync(command, ct);
            return Results.Created($"/catalog/products/{sku}", new { sku });
        });

        // Bulk import (CSV upload, parsed client-side into rows). Always 200: the body reports how many
        // rows landed and which failed, so a hand-edited file with a few bad lines still imports the rest.
        products.MapPost("/import", async (
            ImportProductsCommand command, ImportProductsHandler handler, CancellationToken ct) =>
            Results.Ok(await handler.HandleAsync(command, ct)));

        // List (optionally by category).
        products.MapGet("/", async (
            ProductCategory? category, ListProductsHandler handler, CancellationToken ct) =>
            Results.Ok(await handler.HandleAsync(new ListProductsQuery(category), ct)));

        // Read one.
        products.MapGet("/{sku}", async (
            string sku, GetProductHandler handler, CancellationToken ct) =>
        {
            var dto = await handler.HandleAsync(new GetProductQuery(sku), ct);
            return dto is null ? Results.NotFound() : Results.Ok(dto);
        });

        // Rename a product.
        products.MapPost("/{sku}/rename", async (
            string sku, RenameProductRequest request, RenameProductHandler handler, CancellationToken ct) =>
        {
            await handler.HandleAsync(new RenameProductCommand(sku, request.Name), ct);
            return Results.NoContent();
        });

        // Change a product's storage requirement.
        products.MapPost("/{sku}/storage", async (
            string sku, ChangeStorageRequest request, ChangeProductStorageHandler handler, CancellationToken ct) =>
        {
            await handler.HandleAsync(
                new ChangeProductStorageCommand(sku, request.Storage, request.MinCelsius, request.MaxCelsius), ct);
            return Results.NoContent();
        });

        return app;
    }
}

internal sealed record RenameProductRequest(string Name);

internal sealed record ChangeStorageRequest(string Storage, decimal? MinCelsius, decimal? MaxCelsius);
