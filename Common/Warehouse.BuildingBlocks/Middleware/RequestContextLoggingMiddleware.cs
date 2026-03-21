using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Warehouse.BuildingBlocks.Middleware;

public static class RequestContextLoggingMiddleware
{
	private const string CorrelationIdHeaderName = "X-Correlation-Id";

	private static string GetCorrelationId(HttpContext context)
	{
		context.Request.Headers.TryGetValue(
			CorrelationIdHeaderName,
			out var correlationId);

		return correlationId.FirstOrDefault() ?? context.TraceIdentifier;
	}

	public static IApplicationBuilder UseRequestContextLogging(this IApplicationBuilder app)
	{
		app.Use(async (context, next) =>
		{
			var correlationId = GetCorrelationId(context);

			var loggerFactory = context.RequestServices.GetRequiredService<ILoggerFactory>();
			var logger = loggerFactory.CreateLogger("Warehouse.Middleware.Logging");

			context.Response.OnStarting(() =>
			{
				if (!context.Response.Headers.ContainsKey(CorrelationIdHeaderName))
				{
					context.Response.Headers.Append(CorrelationIdHeaderName, correlationId);
				}
				return Task.CompletedTask;
			});

			var userAgent = context.Request.Headers[HeaderNames.UserAgent].ToString();
			using (logger.BeginScope(new Dictionary<string, object>
			       {
				       ["CorrelationId"] = correlationId,
				       ["UserAgent"] = string.IsNullOrEmpty(userAgent) ? "unknown" : userAgent			       }))
			{
				logger.LogInformation("HTTP {Method} {Path} started with CorrelationId: {CorrelationId}",
					context.Request.Method, context.Request.Path, correlationId);

				await next();
			}
		});

		return app;
	}
}
