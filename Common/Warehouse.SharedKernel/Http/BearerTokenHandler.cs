using Microsoft.AspNetCore.Http;

namespace Warehouse.SharedKernel.Http;

public class BearerTokenHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
	protected override async Task<HttpResponseMessage> SendAsync(
		HttpRequestMessage request,
		CancellationToken cancellationToken)
	{
		var context = httpContextAccessor.HttpContext;
		if (context == null) return await base.SendAsync(request, cancellationToken);

		var authHeader = context.Request.Headers["Authorization"].ToString();

		if (!string.IsNullOrWhiteSpace(authHeader))
		{
			request.Headers.TryAddWithoutValidation("Authorization", authHeader);
		}

		return await base.SendAsync(request, cancellationToken);
	}
}

