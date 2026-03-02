using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Warehouse.Contracts;
using Warehouse.Inventory.Application.Http;
using Warehouse.SharedKernel;
using Warehouse.Inventory.Domain.Interfaces;
using Warehouse.Inventory.Infrastructure.Http;
using Warehouse.Inventory.Infrastructure.Persistence;
using Warehouse.Inventory.Infrastructure.Persistence.InventoryRepository;
using Warehouse.Inventory.Infrastructure.Settings;
using Warehouse.SharedKernel.Http;
using Warehouse.SharedKernel.Security;
using Wolverine;
using Wolverine.RabbitMQ;

namespace Warehouse.Inventory.Infrastructure;

public static class DependencyInjection
{
	public static IServiceCollection AddInfrastructure(this WebApplicationBuilder builder)
	{
		var services = builder.Services;
		services.AddDbContext<InventoryDbContext>(options =>
			options.UseNpgsql(
				builder.Configuration.GetConnectionString("DefaultConnection"),
				x =>
				{
					x.MigrationsAssembly("Warehouse.Inventory.Infrastructure");
					x.EnableRetryOnFailure(5);
				})

			);

		services.AddScoped<IInventoryRepository, InventoryRepository>();

		services.AddScoped<IUnitOfWork, UnitOfWork>();

		services.AddHttpContextAccessor();
		services.AddScoped<IUserContext, UserContext>();
		services.AddTransient<BearerTokenHandler>();

		var productApiOptions = builder.Configuration.GetSection(ProductApiOptions.SectionName)
			.Get<ProductApiOptions>();

		services.AddHttpClient<IProductServiceClient, ProductServiceClient>(client =>
			{
				client.BaseAddress = new Uri(productApiOptions!.BaseUrl);
				client.Timeout = TimeSpan.FromSeconds(productApiOptions.TimeoutSeconds);
			})
			.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
			{
				ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
			})
			.AddHttpMessageHandler<BearerTokenHandler>()
			.AddStandardResilienceHandler(options =>
			{
				options.Retry.MaxRetryAttempts = productApiOptions!.MaxRetryAttempts;
				options.Retry.BackoffType = DelayBackoffType.Exponential;
				options.Retry.UseJitter = true;
			});

		builder.Host.UseWolverine(options =>
		{
			var rabbitMqSettings = builder.Configuration
				.GetSection(RabbitMqSettings.SectionName)
				.Get<RabbitMqSettings>()!;

			options.UseRabbitMq(rabbit =>
			{
				rabbit.HostName = rabbitMqSettings.HostName;
				rabbit.UserName = rabbitMqSettings.UserName;
				rabbit.Password = rabbitMqSettings.Password;
				rabbit.Port = rabbitMqSettings.Port;
			});

			// TODO: Make more 'class configurable' instead of always remember to copy code here.
			options.PublishMessage<ProductInventoryAddedEvent>()
				.ToRabbitExchange("product-inventory-events");
		});

		return services;
	}
}
