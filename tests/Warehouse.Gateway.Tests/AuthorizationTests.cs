using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Warehouse.ServiceDefaults;
using Xunit;

namespace Warehouse.Gateway.Tests;

/// <summary>
/// Integration cover for the shared authorization wiring (<see cref="AuthenticationExtensions"/>): the
/// Desk / Terminal / Staff role policies, the zero-trust fallback, the anonymous infra endpoints, and the
/// <see cref="KeycloakRolesClaimsTransformation"/> that feeds them. Boots an in-memory host on the real
/// <c>AddWarehouseJwtAuth</c> wiring, then swaps the Keycloak bearer for a test scheme that injects a
/// Keycloak-shaped <c>realm_access</c> claim from a header — so the real transformation and the real
/// policies run, without needing Keycloak. (Signature/issuer/audience are covered separately.)
/// </summary>
public sealed class AuthorizationTests : IAsyncLifetime
{
    private WebApplication _app = null!;
    private HttpClient _client = null!;

    public async ValueTask InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();

        // The real shared wiring: JWT policies + role transformation + zero-trust fallback.
        builder.AddWarehouseJwtAuth(requireAuthenticatedByDefault: true);

        // MapDefaultEndpoints maps health checks; they need the service registered.
        builder.Services.AddHealthChecks();

        // Swap the default scheme to a test handler (the Keycloak bearer stays registered but unused).
        builder.Services.AddAuthentication(options =>
        {
            options.DefaultScheme = TestAuthHandler.SchemeName;
            options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
        }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });

        _app = builder.Build();
        _app.UseAuthentication();
        _app.UseAuthorization();

        _app.MapDefaultEndpoints(); // /health, /alive, /version -> AllowAnonymous
        _app.MapGet("/desk", () => "ok").RequireAuthorization(AppRoles.DeskPolicy);
        _app.MapGet("/terminal", () => "ok").RequireAuthorization(AppRoles.TerminalPolicy);
        _app.MapGet("/staff", () => "ok").RequireAuthorization(AppRoles.StaffPolicy);
        _app.MapGet("/business", () => "ok"); // no explicit auth -> the zero-trust fallback gates it

        await _app.StartAsync();
        _client = _app.GetTestClient();
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _app.DisposeAsync();
    }

    [Theory]
    // Infra endpoints stay anonymous even under the zero-trust fallback.
    [InlineData("/version", null, 200)]
    [InlineData("/health", null, 200)]
    // Fallback floor: no token -> 401; a token with no warehouse role -> 403; any warehouse role -> 200.
    [InlineData("/business", null, 401)]
    [InlineData("/business", "guest", 403)]
    [InlineData("/business", "operator", 200)]
    [InlineData("/business", "manager", 200)]
    // Desk policy: desk roles pass, terminal roles are forbidden.
    [InlineData("/desk", "manager", 200)]
    [InlineData("/desk", "coordinator", 200)]
    [InlineData("/desk", "operator", 403)]
    [InlineData("/desk", null, 401)]
    // Terminal policy: terminal roles pass, desk roles are forbidden.
    [InlineData("/terminal", "operator", 200)]
    [InlineData("/terminal", "forklift", 200)]
    [InlineData("/terminal", "manager", 403)]
    // Staff policy: either hub passes.
    [InlineData("/staff", "manager", 200)]
    [InlineData("/staff", "forklift", 200)]
    [InlineData("/staff", "guest", 403)]
    public async Task Endpoint_enforces_the_expected_role_policy(string path, string? roles, int expected)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        if (roles is not null)
        {
            // A caller with the given realm roles (comma-separated); an empty/omitted header => no token.
            request.Headers.Add(TestAuthHandler.RolesHeader, roles);
        }

        var response = await _client.SendAsync(request);

        Assert.Equal(expected, (int)response.StatusCode);
    }

    /// <summary>Authenticates from an <c>X-Test-Roles</c> header, emitting the same nested
    /// <c>realm_access.roles</c> claim a Keycloak token carries — so the real transformation flattens it.</summary>
    private sealed class TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        public const string SchemeName = "Test";
        public const string RolesHeader = "X-Test-Roles";

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue(RolesHeader, out var raw))
            {
                return Task.FromResult(AuthenticateResult.NoResult()); // no token -> unauthenticated (401)
            }

            var roles = raw.ToString()
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(r => $"\"{r}\"");
            var identity = new ClaimsIdentity(authenticationType: SchemeName); // IsAuthenticated = true
            identity.AddClaim(new Claim("realm_access", $"{{\"roles\":[{string.Join(',', roles)}]}}"));

            var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
