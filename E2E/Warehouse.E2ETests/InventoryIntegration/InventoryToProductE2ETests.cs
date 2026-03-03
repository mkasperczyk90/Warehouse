using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Warehouse.Inventory.Api.Controllers.Inventory.CreateInventory;
using Warehouse.Inventory.Infrastructure.Persistence;
using Warehouse.Product.Infrastructure.Persistence;
using DomainProduct = Warehouse.Product.Domain.Products.Entities.Product;

namespace Warehouse.E2ETests.InventoryIntegration;

[CollectionDefinition("E2E")]
public class E2ECollection : ICollectionFixture<E2EEnvironmentFixture> { }

[Collection("E2E")]
public class InventoryToProductE2ETests
{
    private readonly E2EEnvironmentFixture _env;

    public InventoryToProductE2ETests(E2EEnvironmentFixture env) => _env = env;

    [Fact]
    public async Task PostInventory_ShouldIncreaseProductAmountInProductService()
    {
        var inventoryClient = _env.InventoryFactory.CreateClient();
        _ = _env.ProductFactory.CreateClient();

        // Arrange
        await SetInventoryDatabase();
        var productId = (await SetProductDatabase())!.Id;
        const int quantityToAdd = 15;


        inventoryClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Test", "read,write");

        var request = new CreateInventoryRequest { ProductId = productId.Value, Quantity = quantityToAdd };

        // Act
        var response = await inventoryClient.PostAsJsonAsync("/inventories", request);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"API returns error {response.StatusCode}. Details: {errorContent}");
        }

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        await WaitForConditionAsync(async () =>
        {
            using var scope = _env.ProductFactory.Services.CreateScope();
            var productDb = scope.ServiceProvider.GetRequiredService<ProductDbContext>();

            var updatedProduct = await productDb.Products
                .AsNoTracking() // do not use cache
                .FirstOrDefaultAsync(p => p.Id == productId);

            return updatedProduct != null && updatedProduct.Amount == quantityToAdd;

        }, timeoutSeconds: 10, pollIntervalMs: 200);

        using (var scope = _env.ProductFactory.Services.CreateScope())
        {
            var productDb = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
            var finalProduct = await productDb.Products.FindAsync(productId);

            finalProduct.ShouldNotBeNull();
            finalProduct.Amount.ShouldBe(quantityToAdd, "because ProductService should consume event and add quantity.");
        }
    }

    private async Task SetInventoryDatabase()
    {
	    using var scope = _env.InventoryFactory.Services.CreateScope();

	    // TODO: Should not clear database always! Its temporary solution
	    var inventoryDb = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
	    await inventoryDb.Database.EnsureDeletedAsync();
	    await inventoryDb.Database.EnsureCreatedAsync();
    }

    private async Task<DomainProduct?> SetProductDatabase()
    {
        DomainProduct? product = null;

        using var scope = _env.ProductFactory.Services.CreateScope();
        var productDb = scope.ServiceProvider.GetRequiredService<ProductDbContext>();

        // TODO: Should not clear database always! Its temporary solution
        await productDb.Database.EnsureDeletedAsync();
        await productDb.Database.EnsureCreatedAsync();

        product = DomainProduct.Create("E2E Test Product", 0, "description");
        productDb.Products.Add(product);
        await productDb.SaveChangesAsync();

        return product;
    }

    private static async Task WaitForConditionAsync(Func<Task<bool>> condition, int timeoutSeconds, int pollIntervalMs)
    {
        var timeout = DateTime.UtcNow.AddSeconds(timeoutSeconds);
        while (DateTime.UtcNow < timeout)
        {
            if (await condition())
            {
                return;
            }
            await Task.Delay(pollIntervalMs);
        }

        throw new TimeoutException($"Condition was not met within {timeoutSeconds} seconds.");
    }
}
