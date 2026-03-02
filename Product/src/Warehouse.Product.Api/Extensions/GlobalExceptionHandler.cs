using System.Diagnostics;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Warehouse.SharedKernel.Exceptions;

namespace Warehouse.Product.Api.Extensions;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
	public async ValueTask<bool> TryHandleAsync(
		HttpContext httpContext,
		Exception exception,
		CancellationToken cancellationToken)
	{
		var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;

		logger.LogError(exception, "An unhandled exception occurred. TraceId: {TraceId}", traceId);

		var (statusCode, title) = exception switch
		{
			ValidationException => (StatusCodes.Status400BadRequest, "Validation Error"),
			DomainException => (StatusCodes.Status400BadRequest, "Domain Error"),
			UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
			KeyNotFoundException => (StatusCodes.Status404NotFound, "Resource Not Found"),
			_ => (StatusCodes.Status500InternalServerError, "Server Error")
		};

		var problemDetails = new ProblemDetails
		{
			Status = statusCode,
			Title = title,
			Detail = statusCode == StatusCodes.Status500InternalServerError
				? "An unexpected error occurred. Please use the Trace ID to contact support."
				: exception.Message,
			Instance = $"{httpContext.Request.Method} {httpContext.Request.Path}"
		};

		if (exception is ValidationException validationException)
		{
			var errors = validationException.Errors
				.GroupBy(e => e.PropertyName)
				.ToDictionary(
					g => g.Key,
					g => g.Select(e => e.ErrorMessage).ToArray()
				);

			problemDetails.Extensions.Add("errors", errors);
		}
		problemDetails.Extensions.Add("traceId", traceId);
		httpContext.Response.StatusCode = statusCode;

		await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

		return true;
	}
}
