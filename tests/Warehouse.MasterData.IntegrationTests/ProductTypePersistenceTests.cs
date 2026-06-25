using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Warehouse.MasterData.Catalog.Application.Abstractions;
using Warehouse.MasterData.Catalog.Domain;
using Warehouse.SharedKernel.Application;
using Xunit;

namespace Warehouse.MasterData.IntegrationTests;

public sealed class ProductTypePersistenceTests(CatalogDatabaseFixture fixture)
    : CatalogIntegrationTest(fixture)
{
    private async Task<Sku> PersistAsync(ProductType product)
    {
        await using var scope = Fixture.NewScope();
        scope.ServiceProvider.GetRequiredService<IProductTypeRepository>().Add(product);
        await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
        return product.Sku;
    }

    [Fact]
    public async Task ProductType_round_trips_through_postgres_with_its_value_objects_and_owned_collection()
    {
        var sku = await PersistAsync(Sample.Product());

        await using var scope = Fixture.NewScope();
        var reloaded = await scope.ServiceProvider.GetRequiredService<IProductTypeRepository>().GetByIdAsync(sku);

        Assert.NotNull(reloaded);
        Assert.Equal("MILK-1L", reloaded!.Sku.Value);
        Assert.Equal("4006381333931", reloaded.Ean?.Value);
        Assert.Equal(ProductCategory.Refrigerated, reloaded.Category);
        Assert.Equal("pcs", reloaded.BaseUnit.Code);
        Assert.Equal(1.03m, reloaded.UnitWeight.Kilograms);
        Assert.Equal(20, reloaded.Dimensions.HeightCm);

        // Owned storage requirement, including the nested temperature range.
        Assert.True(reloaded.Storage.RequiresColdChain);
        Assert.NotNull(reloaded.Storage.Temperature);
        Assert.Equal(2, reloaded.Storage.Temperature!.MinCelsius);
        Assert.Equal(6, reloaded.Storage.Temperature.MaxCelsius);

        // Owned child collection.
        var conversion = Assert.Single(reloaded.UnitConversions);
        Assert.Equal("ctn", conversion.Unit.Code);
        Assert.Equal(24, conversion.FactorToBase);
    }

    [Fact]
    public async Task A_duplicate_ean_is_rejected_by_the_unique_index()
    {
        await PersistAsync(Sample.Product("MILK-1L", ean: "4006381333931"));

        await Assert.ThrowsAsync<DbUpdateException>(
            () => PersistAsync(Sample.Product("MILK-2L", ean: "4006381333931", withConversion: false)));
    }

    [Fact]
    public async Task Concurrent_updates_are_rejected_by_the_xmin_concurrency_token()
    {
        var sku = await PersistAsync(Sample.Product());

        await using var scopeA = Fixture.NewScope();
        await using var scopeB = Fixture.NewScope();
        var productsA = scopeA.ServiceProvider.GetRequiredService<IProductTypeRepository>();
        var productsB = scopeB.ServiceProvider.GetRequiredService<IProductTypeRepository>();

        // Both load the same row (same xmin), then both try to rename it.
        var productA = await productsA.GetByIdAsync(sku);
        var productB = await productsB.GetByIdAsync(sku);

        productA!.Rename("Skim milk");
        await scopeA.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();

        productB!.Rename("Buttermilk");
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            () => scopeB.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync());
    }
}
