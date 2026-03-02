using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Warehouse.Inventory.Api.Settings;

namespace Warehouse.Inventory.Api.Extensions;

internal static class OpenTelemetryExtensions
{
	internal static void AddOpenTelemetryLogging(this IHostApplicationBuilder applicationBuilder)
	{

		var applicationSettings = applicationBuilder.Configuration
			.GetSection(ApplicationSettings.SectionName)
			.Get<ApplicationSettings>()!;

		applicationBuilder.Services.AddOpenTelemetry()
			.ConfigureResource(resource => resource.AddService(applicationSettings.Name))
			.WithTracing(tracing =>
			{
				tracing.AddAspNetCoreInstrumentation();
				tracing.AddHttpClientInstrumentation();
				tracing.AddOtlpExporter();
			})
			.WithMetrics(metrics =>
			{
				metrics.AddAspNetCoreInstrumentation();
				metrics.AddHttpClientInstrumentation();
				metrics.AddRuntimeInstrumentation();
				metrics.AddOtlpExporter();
			});
		applicationBuilder.Logging.AddOpenTelemetry(logging =>
			{
				logging.IncludeFormattedMessage = true;
				logging.IncludeScopes = true;
				logging.AddOtlpExporter();
			});
	}
}
