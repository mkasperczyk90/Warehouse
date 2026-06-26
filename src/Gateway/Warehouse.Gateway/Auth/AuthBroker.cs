using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Warehouse.Gateway.Auth;

/// <summary>
/// Brokers the desk's badge sign-in: posts the scanned badge to Keycloak's token endpoint (the custom
/// <c>badge-authenticator</c> direct-grant flow resolves the user — no password), keeping the confidential
/// client secret server-side, and returns the bearer token plus the desk user shaped from its claims.
/// </summary>
public sealed class AuthBroker(IHttpClientFactory httpClientFactory, IConfiguration configuration)
{
    public const string KeycloakClient = "keycloak";

    public async Task<LoginResponse?> LoginAsync(string badge, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(badge))
        {
            return null;
        }

        var realm = configuration["Keycloak:Realm"] ?? "warehouse";
        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = configuration["Keycloak:ClientId"] ?? "warehouse-admin",
            ["client_secret"] = configuration["Keycloak:ClientSecret"] ?? string.Empty,
            ["scope"] = "openid",
            // The badge authenticator reads either field; sending both keeps the token endpoint's
            // password-grant pre-checks happy.
            ["badge"] = badge.Trim(),
            ["username"] = badge.Trim(),
        };

        var client = httpClientFactory.CreateClient(KeycloakClient);
        using var response = await client.PostAsync(
            $"realms/{realm}/protocol/openid-connect/token", new FormUrlEncodedContent(form), cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return null; // unknown/disabled badge → 401 at the endpoint
        }

        var token = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken);
        if (token is null || string.IsNullOrEmpty(token.AccessToken))
        {
            return null;
        }

        return new LoginResponse(token.AccessToken, token.RefreshToken, token.ExpiresIn, AuthClaims.ToUser(token.AccessToken));
    }

    private sealed record TokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("refresh_token")] string? RefreshToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn);
}
