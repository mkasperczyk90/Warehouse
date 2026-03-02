using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Warehouse.Inventory.Application.Http;
using Warehouse.Inventory.Infrastructure.Http;
using ProductProgram = Warehouse.Product.Api.Program;
using InventoryProgram = Warehouse.Inventory.Api.Program;
namespace Warehouse.E2ETests;

public class E2EEnvironmentFixture : IAsyncLifetime
{
	public PostgreSqlContainer ProductDbPostgresql { get; }
    public PostgreSqlContainer InventoryDbPostgresql { get; }
    public RabbitMqContainer RabbitMq { get; }

    public WebApplicationFactory<InventoryProgram> InventoryFactory { get; private set; } = null!;
    public WebApplicationFactory<ProductProgram> ProductFactory { get; private set; } = null!;

    public E2EEnvironmentFixture()
    {
	    ProductDbPostgresql = new PostgreSqlBuilder("postgres:16")
		    .WithDatabase("product_db")
		    .WithUsername("postgres")
		    .WithPassword("postgres")
		    .Build();

	    InventoryDbPostgresql = new PostgreSqlBuilder("postgres:16")
		    .WithDatabase("inventory_db")
		    .WithUsername("postgres")
		    .WithPassword("postgres")
		    .Build();

        RabbitMq = new RabbitMqBuilder("rabbitmq:4-management")
            .WithUsername("guest")
            .WithPassword("guest")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await Task.WhenAll(InventoryDbPostgresql.StartAsync(), ProductDbPostgresql.StartAsync(), RabbitMq.StartAsync());

        ProductFactory = new WebApplicationFactory<ProductProgram>()
            .WithWebHostBuilder(builder => ConfigureHost(builder, ProductDbPostgresql));

        var productApiHandler = ProductFactory.Server.CreateHandler();

        InventoryFactory = new WebApplicationFactory<InventoryProgram>()
	        .WithWebHostBuilder(builder =>
	        {
		        ConfigureHost(builder, InventoryDbPostgresql);

		        builder.ConfigureTestServices(services =>
		        {
			        services.AddHttpClient<IProductServiceClient, ProductServiceClient>(client =>
				        {
					        client.BaseAddress = new Uri("http://localhost");
				        })
				        .ConfigurePrimaryHttpMessageHandler(() => productApiHandler);
		        });
	        });
    }

    private void ConfigureHost(IWebHostBuilder builder, PostgreSqlContainer dbContainer)
    {
        builder.UseEnvironment("E2ETesting");
        builder.ConfigureLogging(logging =>
        {
	        logging.ClearProviders();
	        logging.AddConsole();
	        logging.SetMinimumLevel(LogLevel.Debug);
        });
        builder.ConfigureTestServices(services =>
        {
	        services.AddAuthentication(defaultScheme: "Test")
		        .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", options => { });
        });
        builder.UseSetting("ConnectionStrings:DefaultConnection", dbContainer.GetConnectionString());
        builder.UseSetting("RabbitMq:HostName", RabbitMq.Hostname);
        builder.UseSetting("RabbitMq:Port", RabbitMq.GetMappedPublicPort(5672).ToString());
        builder.UseSetting("RabbitMq:UserName", "guest");
        builder.UseSetting("RabbitMq:Password", "guest");
    }

    public async Task DisposeAsync()
    {
        await InventoryFactory.DisposeAsync();
        await ProductFactory.DisposeAsync();
        await ProductDbPostgresql.DisposeAsync();
        await InventoryDbPostgresql.DisposeAsync();
        await RabbitMq.DisposeAsync();
    }
}
