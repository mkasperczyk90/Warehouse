namespace Warehouse.Inventory.Api.Security;

public class DockerKeycloakHttpHandler : DelegatingHandler
{
	public DockerKeycloakHttpHandler() =>
		InnerHandler = new HttpClientHandler
		{
			ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
		};

	protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		if (request.RequestUri?.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) == true)
		{
			request.Headers.Host = request.RequestUri.Authority;

			var builder = new UriBuilder(request.RequestUri)
			{
				Host = "keycloak"
			};
			request.RequestUri = builder.Uri;
		}

		return base.SendAsync(request, cancellationToken);
	}
}
