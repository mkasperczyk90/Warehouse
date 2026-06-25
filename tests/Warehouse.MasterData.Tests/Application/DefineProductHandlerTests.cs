using Warehouse.Contracts.Catalog;
using Warehouse.MasterData.Catalog.Application.Products.DefineProduct;
using Warehouse.MasterData.Catalog.Domain;
using Warehouse.MasterData.Tests.TestDoubles;
using Warehouse.SharedKernel.ValueObjects;
using Xunit;

namespace Warehouse.MasterData.Tests.Application;

public sealed class DefineProductHandlerTests
{
    [Fact]
    public async Task Define_persists_the_product_and_publishes_ProductDefined()
    {
        var products = new FakeProductTypeRepository();
        var outbox = Outbox.Create();
        var handler = new DefineProductHandler(products, outbox);

        var sku = await handler.HandleAsync(Build.DefineCommand());

        Assert.Equal("SKU-1", sku);
        var saved = Assert.Single(products.Saved);
        Assert.Equal("SKU-1", saved.Sku.Value);

        var published = outbox.PublishedMessage<ProductDefinedV2>();
        Assert.Equal("SKU-1", published.Sku);
        Assert.Equal("pcs", published.BaseUnit);
        Assert.Equal(1, published.UnitWeightKg);
        Assert.Equal(0.001m, published.UnitVolumeM3);  // 10×10×10 cm³ → m³
        Assert.Null(published.MinCelsius);              // ambient: no temperature requirement
        Assert.Null(published.MaxCelsius);
    }

    [Fact]
    public async Task Define_coldchain_publishes_the_temperature_range_so_inventory_can_enforce_it()
    {
        var outbox = Outbox.Create();
        var handler = new DefineProductHandler(new FakeProductTypeRepository(), outbox);

        await handler.HandleAsync(Build.DefineCommand(category: "Refrigerated", storage: "ColdChain", min: 0, max: 5));

        var published = outbox.PublishedMessage<ProductDefinedV2>();
        Assert.True(published.RequiresColdChain);
        Assert.Equal(0, published.MinCelsius);
        Assert.Equal(5, published.MaxCelsius);
    }

    [Fact]
    public async Task Define_rejects_a_duplicate_sku()
    {
        var products = new FakeProductTypeRepository();
        products.Seed(Build.Product("SKU-1"));
        var handler = new DefineProductHandler(products, Outbox.Create());

        await Expect.DomainErrorAsync(
            "product_sku_duplicate", () => handler.HandleAsync(Build.DefineCommand("SKU-1")));
    }

    [Fact]
    public async Task Define_rejects_a_duplicate_ean()
    {
        var products = new FakeProductTypeRepository();
        products.Seed(ProductType.Define(
            Sku.Of("SKU-0"), "Other", Ean.Of("4006381333931"), ProductCategory.DryGoods,
            Dimensions.Of(10, 10, 10), Weight.FromKilograms(1), UnitOfMeasure.Piece,
            StorageRequirement.Ambient, isBatchTracked: false, hasExpiryDate: false));
        var handler = new DefineProductHandler(products, Outbox.Create());

        await Expect.DomainErrorAsync(
            "product_ean_duplicate",
            () => handler.HandleAsync(Build.DefineCommand("SKU-1", ean: "4006381333931")));
    }

    [Fact]
    public async Task Define_maps_a_cold_chain_requirement_from_the_command()
    {
        var products = new FakeProductTypeRepository();
        var handler = new DefineProductHandler(products, Outbox.Create());

        await handler.HandleAsync(Build.DefineCommand(category: "Refrigerated", storage: "ColdChain", min: 0, max: 5));

        Assert.True(products.Saved.Single().Storage.RequiresColdChain);
    }

    [Fact]
    public async Task Define_rejects_an_unknown_category()
    {
        var handler = new DefineProductHandler(new FakeProductTypeRepository(), Outbox.Create());

        await Expect.DomainErrorAsync(
            "category_unknown", () => handler.HandleAsync(Build.DefineCommand(category: "Nonsense")));
    }
}
