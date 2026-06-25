using Warehouse.MasterData.Catalog.Application.Products.ChangeProductStorage;
using Warehouse.MasterData.Catalog.Application.Products.RenameProduct;
using Warehouse.MasterData.Catalog.Domain;
using Warehouse.MasterData.Tests.TestDoubles;
using Warehouse.SharedKernel.ValueObjects;
using Xunit;

namespace Warehouse.MasterData.Tests.Application;

public sealed class ProductCommandHandlerTests
{
    // --- RenameProduct ------------------------------------------------------
    [Fact]
    public async Task Rename_updates_the_label_and_commits_once()
    {
        var products = new FakeProductTypeRepository();
        products.Seed(Build.Product("SKU-1"));
        var uow = new FakeUnitOfWork();
        var handler = new RenameProductHandler(products, uow);

        await handler.HandleAsync(new RenameProductCommand("SKU-1", "New name"));

        Assert.Equal("New name", products.Saved.Single().Name);
        Assert.Equal(1, uow.SaveCount);
    }

    [Fact]
    public async Task Rename_throws_for_an_unknown_sku()
    {
        var handler = new RenameProductHandler(new FakeProductTypeRepository(), new FakeUnitOfWork());

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => handler.HandleAsync(new RenameProductCommand("SKU-9", "x")));
    }

    // --- ChangeProductStorage ----------------------------------------------
    [Fact]
    public async Task ChangeStorage_promotes_a_dry_product_to_cold_chain_and_commits_once()
    {
        var products = new FakeProductTypeRepository();
        products.Seed(Build.Product("SKU-1")); // dry goods, ambient
        var uow = new FakeUnitOfWork();
        var handler = new ChangeProductStorageHandler(products, uow);

        await handler.HandleAsync(new ChangeProductStorageCommand("SKU-1", "ColdChain", 0, 5));

        Assert.True(products.Saved.Single().Storage.RequiresColdChain);
        Assert.Equal(1, uow.SaveCount);
    }

    [Fact]
    public async Task ChangeStorage_rejects_dropping_the_cold_chain_on_a_refrigerated_product()
    {
        var products = new FakeProductTypeRepository();
        products.Seed(Build.Product(
            "SKU-1",
            category: ProductCategory.Refrigerated,
            storage: StorageRequirement.ColdChain(TemperatureRange.Of(0, 5))));
        var handler = new ChangeProductStorageHandler(products, new FakeUnitOfWork());

        await Expect.DomainErrorAsync(
            "storage_cold_chain_required",
            () => handler.HandleAsync(new ChangeProductStorageCommand("SKU-1", "Ambient", null, null)));
    }
}
