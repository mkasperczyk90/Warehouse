using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using Warehouse.Inventory.Application.Http;
using Warehouse.Inventory.Infrastructure.Persistence;
using Wolverine;

namespace Inventory.Api.IntegrationTests.Inventories.Api;

public class InventoryTestAppFactory : WebApplicationFactory<Program>
{
	public IHost? HostInstance { get; private set; }

	protected override void ConfigureWebHost(IWebHostBuilder builder)
	{
		builder.UseEnvironment("Testing");

		builder.ConfigureTestServices(services =>
		{
			services.DisableAllExternalWolverineTransports();

			services.AddAuthentication("Test")
				.AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });

			services.RemoveAll(typeof(DbContextOptions<InventoryDbContext>));
			services.RemoveAll(typeof(InventoryDbContext));

			var efInternalServiceProvider = new ServiceCollection()
				.AddEntityFrameworkInMemoryDatabase()
				.BuildServiceProvider();

			services.AddDbContext<InventoryDbContext>(options =>
			{
				options.UseInMemoryDatabase("TestInventoryDb")
					.UseInternalServiceProvider(efInternalServiceProvider);
			});

			services.RemoveAll(typeof(IProductServiceClient));
			var mockClient = Substitute.For<IProductServiceClient>();
			mockClient.ExistsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);

			services.AddSingleton(mockClient);
		});
		// builder.ConfigureServices(services =>
		// {
		// 	services.AddAuthentication("Test")
		// 		.AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });
		//
		// 	services.RemoveAll(typeof(IProductServiceClient));
		//
		// 	services.RemoveAll(typeof(DbContextOptions<InventoryDbContext>));
		// 	services.RemoveAll(typeof(InventoryDbContext));
		// 	services.DisableAllExternalWolverineTransports();
		//
		// 	var efInternalServiceProvider = new ServiceCollection()
		// 		.AddEntityFrameworkInMemoryDatabase()
		// 		.BuildServiceProvider();
		//
		// 	services.AddDbContext<InventoryDbContext>(options =>
		// 	{
		// 		options.UseInMemoryDatabase("TestInventoryDb")
		// 			.UseInternalServiceProvider(efInternalServiceProvider);
		// 	});
		//
		// 	var mockClient = Substitute.For<IProductServiceClient>();
		// 	mockClient.ExistsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);
		//
		// 	services.AddSingleton(mockClient);
		// });
	}

	protected override IHost CreateHost(IHostBuilder builder)
	{
		HostInstance = base.CreateHost(builder);
		builder.UseWolverine(opts =>
		{
			opts.StubAllExternalTransports();
		});
		return HostInstance;
	}
}
