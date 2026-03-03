using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;
using Warehouse.Contracts;
using Warehouse.Product.Infrastructure.Persistence;
using Wolverine.Tracking;
using DomainProduct = Warehouse.Product.Domain.Products.Entities.Product;

namespace Product.Api.IntegrationTests.Products.IntegrationEvents;

public class ProductInventoryAddedConsumerTests : IClassFixture<ProductsTestAppFactory>
{
    private readonly ProductsTestAppFactory _factory;
    private readonly HttpClient _client;

    public ProductInventoryAddedConsumerTests(ProductsTestAppFactory factory)
    {
	    _factory = factory;
	    _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
	    {
		    BaseAddress = new Uri("https://localhost")
	    });

	    _client.DefaultRequestHeaders.Authorization =
		    new System.Net.Http.Headers.AuthenticationHeaderValue("Test");
    }

    [Fact]
    public async Task PostInventories_ShouldReturn201_AndPublishEvent()
    {
	    var product = DomainProduct.Create("prod_1", 5, "description");
	    var eventId = Guid.NewGuid();
	    const int quantityToAdd = 15;

	    var duplicateEvent = new ProductInventoryAddedEvent(eventId, product.Id.Value, quantityToAdd, DateTime.UtcNow);
	    var host = _factory.HostInstance ?? throw new InvalidOperationException("Host nie został zainicjalizowany.");

	    using (var setupScope = _factory.Services.CreateScope())
	    {
		    var setupDb = setupScope.ServiceProvider.GetRequiredService<ProductDbContext>();
		    await setupDb.Database.EnsureCreatedAsync();

		    setupDb.Products.Add(product);
		    await setupDb.SaveChangesAsync();
	    }

	    await host.TrackActivity()
		    .Timeout(TimeSpan.FromSeconds(10))
		    .ExecuteAndWaitAsync(context => context.PublishAsync(duplicateEvent));

	    await host.TrackActivity()
		    .Timeout(TimeSpan.FromSeconds(10))
		    .ExecuteAndWaitAsync(context => context.PublishAsync(duplicateEvent));

	    using var scope = _factory.Services.CreateScope();
	    var db = scope.ServiceProvider.GetRequiredService<ProductDbContext>();

	    var createdProduct = db.Products.FirstOrDefault(x => x.Id == product.Id);

	    createdProduct.ShouldNotBeNull("because the first event delivery should create the product record.");

	    createdProduct.Amount.ShouldBe(quantityToAdd,
		    "because the handler must be idempotent and ignore duplicate events.");
    }
}
