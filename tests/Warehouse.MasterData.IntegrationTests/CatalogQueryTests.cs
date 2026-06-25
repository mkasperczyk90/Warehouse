using Microsoft.Extensions.DependencyInjection;
using Warehouse.MasterData.Catalog.Application.Abstractions;
using Warehouse.MasterData.Catalog.Application.Products.GetProduct;
using Warehouse.MasterData.Catalog.Application.Products.ListProducts;
using Warehouse.MasterData.Catalog.Domain;
using Warehouse.MasterData.Catalog.Infrastructure.Persistence;
using Warehouse.SharedKernel.Application;
using Xunit;

namespace Warehouse.MasterData.IntegrationTests;

public sealed class CatalogQueryTests(CatalogDatabaseFixture fixture) : CatalogIntegrationTest(fixture)
{
    private async Task SeedAsync(params ProductType[] products)
    {
        await using var scope = Fixture.NewScope();
        var repo = scope.ServiceProvider.GetRequiredService<IProductTypeRepository>();
        foreach (var p in products)
        {
            repo.Add(p);
        }

        await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
    }

    [Fact]
    public async Task GetProductHandler_reads_the_full_card_back_as_a_dto()
    {
        await SeedAsync(Sample.Product());

        await using var scope = Fixture.NewScope();
        var handler = new GetProductHandler(scope.ServiceProvider.GetRequiredService<CatalogDbContext>());

        var dto = await handler.HandleAsync(new GetProductQuery("MILK-1L"));

        Assert.NotNull(dto);
        Assert.Equal("MILK-1L", dto!.Sku);
        Assert.Equal("Refrigerated", dto.Category);
        Assert.Equal("pcs", dto.BaseUnit);
        Assert.Equal("ColdChain", dto.Storage.Mode);
        Assert.Equal(2, dto.Storage.MinCelsius);
        Assert.Equal(6, dto.Storage.MaxCelsius);
        var conversion = Assert.Single(dto.UnitConversions);
        Assert.Equal("ctn", conversion.Unit);
        Assert.Equal(24, conversion.FactorToBase);
    }

    [Fact]
    public async Task GetProductHandler_returns_null_for_an_unknown_sku()
    {
        await using var scope = Fixture.NewScope();
        var handler = new GetProductHandler(scope.ServiceProvider.GetRequiredService<CatalogDbContext>());

        Assert.Null(await handler.HandleAsync(new GetProductQuery("NOPE")));
    }

    [Fact]
    public async Task ListProductsHandler_projects_summaries_and_filters_by_category()
    {
        await SeedAsync(Sample.Product(), Sample.DryProduct());

        await using var scope = Fixture.NewScope();
        var handler = new ListProductsHandler(scope.ServiceProvider.GetRequiredService<CatalogDbContext>());

        var all = await handler.HandleAsync(new ListProductsQuery(null));
        var refrigerated = await handler.HandleAsync(new ListProductsQuery(ProductCategory.Refrigerated));
        var frozen = await handler.HandleAsync(new ListProductsQuery(ProductCategory.Frozen));

        Assert.Equal(2, all.Count);
        var one = Assert.Single(refrigerated);
        Assert.Equal("MILK-1L", one.Sku);
        Assert.Equal("Refrigerated", one.Category);
        Assert.Equal("ColdChain", one.Storage);
        Assert.True(one.IsBatchTracked);
        Assert.Empty(frozen);
    }
}
