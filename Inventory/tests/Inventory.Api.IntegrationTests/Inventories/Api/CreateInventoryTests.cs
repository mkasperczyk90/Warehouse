using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;
using Warehouse.Contracts;
using Warehouse.Inventory.Api.Controllers.Inventory.CreateInventory;
using Warehouse.Inventory.Infrastructure.Persistence;
using Wolverine;
using Wolverine.Tracking;

namespace Inventory.Api.IntegrationTests.Inventories.Api;

public class CreateInventoryTests : IClassFixture<InventoryTestAppFactory>
{
    private readonly InventoryTestAppFactory _factory;
    private readonly HttpClient _client;

    public CreateInventoryTests(InventoryTestAppFactory factory)
    {
	    _factory = factory;
	    _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
	    {
		    BaseAddress = new Uri("https://localhost")
	    });
	    // TODO: inmemory RabbitMQ
	    _client.DefaultRequestHeaders.Authorization =
		    new System.Net.Http.Headers.AuthenticationHeaderValue("Test");
    }

    [Fact]
    public async Task PostInventories_WhenValidRequestAndProductExists_ShouldReturn201_AndPublishEvent()
    {
	    using (var scope = _factory.Services.CreateScope())
	    {
		    var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
		    await db.Database.EnsureCreatedAsync();
	    }

        var productId = Guid.NewGuid();
        var request = new CreateInventoryRequest{ ProductId = productId, Quantity = 15 };

        var host = _factory.HostInstance ?? throw new InvalidOperationException("Host does not initialized.");

        var trackedSession = await host.TrackActivity()
	        .Timeout(TimeSpan.FromSeconds(30))
	        .ExecuteAndWaitAsync((Func<IMessageContext, Task>)(async _ =>
	        {
		        var response = await _client.PostAsJsonAsync("/api/v1/inventories", request);
		        response.StatusCode.ShouldBe(HttpStatusCode.Created);
	        }));

        var publishedEvent = trackedSession.Sent
            .MessagesOf<ProductInventoryAddedEvent>()
            .SingleOrDefault();

        publishedEvent.ShouldNotBeNull($"Event {nameof(ProductInventoryAddedEvent)} should be published.");
        publishedEvent!.ProductId.ShouldBe(productId);
        publishedEvent.Quantity.ShouldBe(request.Quantity);
        publishedEvent.EventId.ShouldNotBe(Guid.Empty);
        publishedEvent.OccurredAt.ShouldBeGreaterThan(DateTime.UtcNow.AddSeconds(-5));
    }

    [Fact(Skip = "Not implemented yet")]
    public void PostInventories_WhenValidRequest_ShouldSaveToDatabase_AndCommitUnitOfWork()
    {
    }

    [Fact(Skip = "Not implemented yet")]
    public void PostInventories_WhenProductDoesNotExist_ShouldReturn400_WithProductNotFoundErrorCode()
    {
    }

    [Fact(Skip = "Not implemented yet")]
    public void PostInventories_WhenValidationFails_ShouldThrowValidationException()
    {
    }

    [Fact(Skip = "Not implemented yet")]
    public void PostInventories_WhenUserNotAuthenticated_ShouldReturn401()
    {
    }

    [Fact(Skip = "Not implemented yet")]
    public void PostInventories_WhenUserLacksWriteRole_ShouldReturn403()
    {
    }
}
