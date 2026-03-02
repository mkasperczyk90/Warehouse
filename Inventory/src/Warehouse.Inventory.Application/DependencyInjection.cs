using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Warehouse.SharedKernel.Behavior;

namespace Warehouse.Inventory.Application;

public static class DependencyInjection
{
	public static IServiceCollection AddApplicationLayer(this IServiceCollection services)
	{
		services.AddMediatR(cfg => {
			cfg.RegisterServicesFromAssembly(typeof(InventoryApplicationMarker).Assembly);
			cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
		});

		services.AddValidatorsFromAssembly(typeof(InventoryApplicationMarker).Assembly);
		return services;
	}
}
