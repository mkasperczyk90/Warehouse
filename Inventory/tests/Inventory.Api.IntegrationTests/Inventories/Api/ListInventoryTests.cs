using Microsoft.AspNetCore.Mvc.Testing;

namespace Inventory.Api.IntegrationTests.Inventories.Api;

public class ListInventoryTests : IClassFixture<InventoryTestAppFactory>
{
	private readonly InventoryTestAppFactory _factory;
	private readonly HttpClient _client;

	public ListInventoryTests(InventoryTestAppFactory factory)
	{
		_factory = factory;
		_client = _factory.CreateClient(new WebApplicationFactoryClientOptions
		{
			BaseAddress = new Uri("https://localhost")
		});

		_client.DefaultRequestHeaders.Authorization =
			new System.Net.Http.Headers.AuthenticationHeaderValue("Test");
	}

	[Fact(Skip = "Not implemented yet")]
	public void GetInventories_WhenInventoriesExist_ShouldReturn200AndList()
	{
	}

	[Fact(Skip = "Not implemented yet")]
	public void GetInventories_WhenNoInventoriesExist_ShouldReturn200AndEmptyList()
	{
	}

	[Fact(Skip = "Not implemented yet")]
	public void GetInventories_WhenUserNotAuthenticated_ShouldReturn401()
	{
	}

	[Fact(Skip = "Not implemented yet")]
	public void GetInventories_WhenUserLacksReadRole_ShouldReturn403()
	{
	}
}
