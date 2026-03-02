using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using Warehouse.Inventory.Application.Http;
using Warehouse.Inventory.Infrastructure.Persistence;
using Wolverine;

namespace Inventory.Api.IntegrationTests.Inventories;

public class InventoryTestAppFactory : WebApplicationFactory<Program>
{
	public IHost? HostInstance { get; private set; }

	protected override void ConfigureWebHost(IWebHostBuilder builder)
	{
		builder.UseEnvironment("Testing");

		builder.ConfigureServices(services =>
		{
			services.AddAuthentication("Test")
				.AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });

			services.RemoveAll(typeof(IProductServiceClient));

			services.RemoveAll(typeof(DbContextOptions<InventoryDbContext>));
			services.RemoveAll(typeof(InventoryDbContext));

			services.PostConfigure<WolverineOptions>(opts =>
			{
				opts.StubAllExternalTransports();
			});

			var efInternalServiceProvider = new ServiceCollection()
				.AddEntityFrameworkInMemoryDatabase()
				.BuildServiceProvider();

			services.AddDbContext<InventoryDbContext>(options =>
			{
				options.UseInMemoryDatabase("TestInventoryDb")
					.UseInternalServiceProvider(efInternalServiceProvider);
			});

			var mockClient = Substitute.For<IProductServiceClient>();
			mockClient.ExistsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);


			services.AddSingleton(mockClient);
		});
	}

	protected override IHost CreateHost(IHostBuilder builder)
	{
		HostInstance = base.CreateHost(builder);
		return HostInstance;
	}
}
