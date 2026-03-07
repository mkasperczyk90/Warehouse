using OpenTelemetry.Exporter;
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

		applicationBuilder.Logging.ClearProviders();
		var otlpEndpoint = applicationBuilder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];

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

		applicationBuilder.Logging.AddOpenTelemetry(options =>
			{
				options.IncludeFormattedMessage = true;
				options.IncludeScopes = true;

				options.SetResourceBuilder(ResourceBuilder.CreateDefault()
					.AddService(applicationSettings.Name));

				options.AddConsoleExporter();

				if (!string.IsNullOrEmpty(otlpEndpoint))
				{
					options.AddOtlpExporter();
				}
			});
	}
}
