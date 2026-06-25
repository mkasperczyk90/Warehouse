using Warehouse.Contracts.Catalog;
using Warehouse.MasterData.Catalog.Application.Products.DefineProduct;
using Warehouse.MasterData.Catalog.Application.Products.ImportProducts;
using Warehouse.MasterData.Catalog.Infrastructure.Persistence;
using Warehouse.MasterData.Tests.TestDoubles;
using Wolverine.EntityFrameworkCore;
using Xunit;

namespace Warehouse.MasterData.Tests.Application;

public sealed class ImportProductsHandlerTests
{
    private static ImportProductsHandler Handler(
        FakeProductTypeRepository products, out IDbContextOutbox<CatalogDbContext> outbox)
    {
        outbox = Outbox.Create();
        return new ImportProductsHandler(new DefineProductHandler(products, outbox));
    }

    [Fact]
    public async Task Import_creates_every_valid_row_and_announces_each_one()
    {
        var products = new FakeProductTypeRepository();
        var handler = Handler(products, out var outbox);

        var result = await handler.HandleAsync(new ImportProductsCommand(
            [Build.DefineCommand("SKU-1"), Build.DefineCommand("SKU-2"), Build.DefineCommand("SKU-3")]));

        Assert.Equal(3, result.Created);
        Assert.Empty(result.Failed);
        Assert.Equal(3, products.Saved.Count);
        Assert.Equal(3, outbox.Published().OfType<ProductDefinedV2>().Count());
    }

    [Fact]
    public async Task Import_isolates_a_bad_row_and_still_lands_the_good_ones()
    {
        var products = new FakeProductTypeRepository();
        var handler = Handler(products, out _);

        var result = await handler.HandleAsync(new ImportProductsCommand(
        [
            Build.DefineCommand("SKU-1"),
            Build.DefineCommand("SKU-2", category: "Nonsense"), // unknown category → rejected
            Build.DefineCommand("SKU-3"),
        ]));

        Assert.Equal(2, result.Created);
        var failure = Assert.Single(result.Failed);
        Assert.Equal(2, failure.Row);              // 1-based position of the bad row
        Assert.Equal("SKU-2", failure.Sku);
        Assert.Equal("category_unknown", failure.Code);
        Assert.Equal(2, products.Saved.Count);
    }

    [Fact]
    public async Task Import_catches_a_duplicate_sku_within_the_same_file()
    {
        var products = new FakeProductTypeRepository();
        var handler = Handler(products, out _);

        var result = await handler.HandleAsync(new ImportProductsCommand(
            [Build.DefineCommand("SKU-1"), Build.DefineCommand("SKU-1")]));

        Assert.Equal(1, result.Created);
        var failure = Assert.Single(result.Failed);
        Assert.Equal("product_sku_duplicate", failure.Code);
    }
}
