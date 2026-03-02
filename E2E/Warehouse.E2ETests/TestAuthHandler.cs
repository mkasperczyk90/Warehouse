using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Warehouse.E2ETests;

public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
	public TestAuthHandler(
		IOptionsMonitor<AuthenticationSchemeOptions> options,
		ILoggerFactory logger,
		UrlEncoder encoder)
		: base(options, logger, encoder) { }

	protected override Task<AuthenticateResult> HandleAuthenticateAsync()
	{
		var authHeader = Context.Request.Headers.Authorization.ToString();

		var roles = new List<string> { "read" };

		if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Test "))
		{
			var parameter = authHeader.Substring("Test ".Length).Trim();
			if (!string.IsNullOrEmpty(parameter))
			{
				roles = parameter.Split(',').Select(r => r.Trim()).ToList();
			}
		}
		var claims = new List<Claim>
		{
			new Claim(ClaimTypes.Name, "TestUser"),
		};

		foreach (var role in roles)
		{
			claims.Add(new Claim(ClaimTypes.Role, role));
		}

		var identity = new ClaimsIdentity(claims, "Test");
		var principal = new ClaimsPrincipal(identity);
		var ticket = new AuthenticationTicket(principal, "Test");

		return Task.FromResult(AuthenticateResult.Success(ticket));
	}
}
