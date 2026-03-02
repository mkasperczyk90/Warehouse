using Warehouse.Product.Api.Settings;
using Warehouse.Product.Infrastructure.Settings;

namespace Warehouse.Product.Api.Extensions;

internal static class SettingExtensions
{
	internal static void AddSettings(this IHostApplicationBuilder applicationBuilder)
	{
		applicationBuilder.Services.AddOptions<ApplicationSettings>()
			.Bind(applicationBuilder.Configuration.GetSection(ApplicationSettings.SectionName))
			.ValidateDataAnnotations()
			.ValidateOnStart();

		applicationBuilder.Services.AddOptions<KeyCloakOptions>()
			.Bind(applicationBuilder.Configuration.GetSection(KeyCloakOptions.SectionName))
			.ValidateDataAnnotations()
			.ValidateOnStart();

		applicationBuilder.Services.AddOptions<RabbitMqSettings>()
			.Bind(applicationBuilder.Configuration.GetSection(RabbitMqSettings.SectionName))
			.ValidateDataAnnotations()
			.ValidateOnStart();
	}
}
