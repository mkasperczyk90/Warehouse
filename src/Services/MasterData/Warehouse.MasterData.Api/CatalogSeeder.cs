using Microsoft.EntityFrameworkCore;
using Warehouse.MasterData.Catalog.Application.Products.DefineProduct;
using Warehouse.MasterData.Catalog.Application.Products.ImportProducts;
using Warehouse.MasterData.Catalog.Infrastructure.Persistence;

namespace Warehouse.MasterData.Api;

/// <summary>
/// Dev-only seed for the product catalog so the real <c>GET /catalog/products</c> returns the same cards
/// the admin panel's MSW fixtures show. It replays the rows through <see cref="ImportProductsHandler"/>
/// (the same path a CSV upload takes), so each product is announced on the outbox — Inventory and
/// Logistics build their product replicas from the resulting <c>ProductDefinedV2</c> events, exactly as in
/// production. Idempotent: it no-ops once any product exists.
/// </summary>
internal static class CatalogSeeder
{
    public static async Task SeedAsync(
        ImportProductsHandler import, CatalogDbContext db, CancellationToken cancellationToken = default)
    {
        if (await db.Products.AnyAsync(cancellationToken))
        {
            return;
        }

        await import.HandleAsync(new ImportProductsCommand(Products), cancellationToken);
    }

    // sku, name, ean, category, L, W, H (cm), weight (kg), baseUnit, storage, minC, maxC, batchTracked, expiry
    private static readonly IReadOnlyList<DefineProductCommand> Products =
    [
        new("MILK-1L", "Whole milk 3.2% — 1 L carton", "4006381333931", "Refrigerated",
            7, 7, 20, 1.03m, "pcs", "ColdChain", 2, 6, true, true),
        new("YOG-400", "Greek yoghurt 400 g", "5901234123457", "Refrigerated",
            9.5m, 9.5m, 6, 0.41m, "pcs", "ColdChain", 2, 6, true, true),
        new("BERRY-1KG", "Frozen berries 1 kg", "5601012009873", "Frozen",
            20, 14, 6, 1, "pcs", "ColdChain", -18, -18, true, true),
        new("CHEESE-5KG", "Cheese wheel 5 kg", "5902860004417", "Refrigerated",
            25, 25, 12, 5, "kg", "ColdChain", 2, 8, true, true),
        new("SOLV-5L", "Cleaning solvent 5 L", null, "Hazardous",
            20, 20, 30, 5.2m, "l", "Hazardous", null, null, true, false),
        new("BOX-L", "Cardboard box L", "5901111000017", "DryGoods",
            60, 40, 40, 0.32m, "pcs", "Ambient", null, null, false, false),
    ];
}
