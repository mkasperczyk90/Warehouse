using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Warehouse.BuildingBlocks.Behavior;

public class LoggingBehavior<TRequest, TResponse>(
	ILogger<LoggingBehavior<TRequest, TResponse>> logger)
	: IPipelineBehavior<TRequest, TResponse>
	where TRequest : IRequest<TResponse>
{
	public async Task<TResponse> Handle(
		TRequest request,
		RequestHandlerDelegate<TResponse> next,
		CancellationToken cancellationToken)
	{
		var requestName = typeof(TRequest).Name;
		var timer = Stopwatch.StartNew();

		logger.LogInformation("Starting request {RequestName} {@Request}",
			requestName, request);

		try
		{
			var response = await next();

			timer.Stop();
			logger.LogInformation("Finished request {RequestName} in {ElapsedMilliseconds}ms",
				requestName, timer.ElapsedMilliseconds);

			return response;
		}
		catch (Exception ex)
		{
			timer.Stop();
			logger.LogError(ex, "Request {RequestName} failed after {ElapsedMilliseconds}ms",
				requestName, timer.ElapsedMilliseconds);
			throw;
		}
	}
}
