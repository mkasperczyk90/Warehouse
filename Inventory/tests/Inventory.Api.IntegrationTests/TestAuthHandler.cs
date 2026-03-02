using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Inventory.Api.IntegrationTests;

public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
	// TODO: Create Common class for test
	public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
		ILoggerFactory logger, UrlEncoder encoder) : base(options, logger, encoder) { }

	protected override Task<AuthenticateResult> HandleAuthenticateAsync()
	{
		var claims = new[] { new Claim(ClaimTypes.Role, "write"), new Claim(ClaimTypes.Name, "TestUser") };
		var identity = new ClaimsIdentity(claims, "Test");
		var principal = new ClaimsPrincipal(identity);
		var ticket = new AuthenticationTicket(principal, "Test");

		return Task.FromResult(AuthenticateResult.Success(ticket));
	}
}
