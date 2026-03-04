using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Warehouse.Product.Infrastructure.Persistence;
using Wolverine;

namespace Product.Api.IntegrationTests.Products.IntegrationEvents;

public class ProductsTestAppFactory : WebApplicationFactory<Program>
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

			services.RemoveAll(typeof(DbContextOptions<ProductDbContext>));
			services.RemoveAll(typeof(ProductDbContext));

			var efInternalServiceProvider = new ServiceCollection()
				.AddEntityFrameworkInMemoryDatabase()
				.BuildServiceProvider();

			services.AddDbContext<ProductDbContext>(options =>
			{
				options.UseInMemoryDatabase("TestProductDb")
					.UseInternalServiceProvider(efInternalServiceProvider);
			});
		});
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
