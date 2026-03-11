using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Warehouse.Product.Application;
using Warehouse.Product.Domain.Interfaces;
using Warehouse.Product.Infrastructure.Persistence;
using Warehouse.Product.Infrastructure.Persistence.ProductRepository;
using Warehouse.SharedKernel;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Warehouse.BuildingBlocks.Behavior;
using Warehouse.BuildingBlocks.EventMessages;
using Warehouse.Product.Application.Products.IntegrationEvents;
using Warehouse.Product.Infrastructure.EventRegistration;
using Warehouse.Product.Infrastructure.Persistence.ProcessedEventRepository;
using Warehouse.Product.Infrastructure.Settings;
using Wolverine;
using Wolverine.ErrorHandling;
using Wolverine.RabbitMQ;

namespace Warehouse.Product.Infrastructure;

public static class DependencyInjection
{
	public static IServiceCollection AddInfrastructure(this WebApplicationBuilder builder)
	{
		var services = builder.Services;
		services.AddDbContext<ProductDbContext>(options =>
			options.UseNpgsql(
				builder.Configuration.GetConnectionString("DefaultConnection"),
				x =>
				{
					x.MigrationsAssembly("Warehouse.Product.Infrastructure");
					x.EnableRetryOnFailure(5);
				})

			);

		services.AddScoped<IProductRepository, ProductRepository>();
		services.AddScoped<IProcessedEventRepository, ProcessedEventRepository>();

		services.AddScoped<IUnitOfWork, UnitOfWork>();

		services.AddMediatR(cfg => {
			cfg.RegisterServicesFromAssembly(typeof(ProductApplicationMarker).Assembly);
			cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
		});

		services.AddValidatorsFromAssembly(typeof(ProductApplicationMarker).Assembly);

		builder.Host.UseWolverine(opts =>
		{
			var rabbitMqSettings = builder.Configuration
				.GetSection(RabbitMqSettings.SectionName)
				.Get<RabbitMqSettings>()!;

			opts.UseRabbitMq(rabbit =>
			{
				rabbit.HostName = rabbitMqSettings.HostName;
				rabbit.UserName = rabbitMqSettings.UserName;
				rabbit.Password = rabbitMqSettings.Password;
				rabbit.Port = rabbitMqSettings.Port;
			}).AutoProvision();

			opts.Discovery.IncludeAssembly(typeof(ProductInventoryAddedConsumer).Assembly);

			opts.Include(new RabbitEventMessageExtension(
				EventMessageConfiguration.PublisherBindings,
				EventMessageConfiguration.ListingBindings));

			opts.OnException<Npgsql.NpgsqlException>()
				.RetryTimes(3)
				.Then.MoveToErrorQueue();

			opts.OnException<Exception>()
				.RetryTimes(1)
				.Then.MoveToErrorQueue();
		});

		return services;
	}
}
