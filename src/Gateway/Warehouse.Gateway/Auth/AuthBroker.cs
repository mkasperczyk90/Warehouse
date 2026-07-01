using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Warehouse.Gateway.Auth;

/// <summary>
/// Brokers the desk's badge sign-in and the token lifecycle against Keycloak, keeping the confidential
/// client secret server-side: <see cref="LoginAsync"/> posts the scanned badge to the custom
/// <c>badge-authenticator</c> direct-grant flow (no password); <see cref="RefreshAsync"/> renews an expiring
/// session from its refresh token; <see cref="LogoutAsync"/> ends the Keycloak session. Login/refresh return
/// the bearer token plus the desk user shaped from its claims.
/// </summary>
public sealed class AuthBroker(IHttpClientFactory httpClientFactory, IConfiguration configuration)
{
    public const string KeycloakClient = "keycloak";

    // The realm URL the gateway also validates against (Keycloak__Authority), so a minted token's issuer
    // lines up with the JwtBearer metadata issuer.
    private string Authority =>
        (configuration["Keycloak:Authority"] ?? "http://localhost:8080/realms/warehouse").TrimEnd('/');

    private string ClientId => configuration["Keycloak:ClientId"] ?? "warehouse-admin";
    private string ClientSecret => configuration["Keycloak:ClientSecret"] ?? string.Empty;

    public async Task<LoginResponse?> LoginAsync(string badge, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(badge))
        {
            return null;
        }

        return await TokenAsync(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["scope"] = "openid",
            // The badge authenticator reads either field; sending both keeps the token endpoint's
            // password-grant pre-checks happy.
            ["badge"] = badge.Trim(),
            ["username"] = badge.Trim(),
        }, cancellationToken);
    }

    /// <summary>Renews the session from its refresh token; null when the refresh token is expired/revoked.</summary>
    public async Task<LoginResponse?> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return null;
        }

        return await TokenAsync(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken,
        }, cancellationToken);
    }

    /// <summary>Ends the Keycloak session (revokes the refresh token). Best-effort — a blank or already
    /// invalid token is treated as success (there is nothing left to revoke).</summary>
    public async Task<bool> LogoutAsync(string refreshToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return true;
        }

        var form = new Dictionary<string, string>
        {
            ["client_id"] = ClientId,
            ["client_secret"] = ClientSecret,
            ["refresh_token"] = refreshToken,
        };

        var client = httpClientFactory.CreateClient(KeycloakClient);
        using var response = await client.PostAsync(
            $"{Authority}/protocol/openid-connect/logout", new FormUrlEncodedContent(form), cancellationToken);

        return response.IsSuccessStatusCode;
    }

    /// <summary>Posts a grant to the token endpoint (client credentials added) and shapes the response.</summary>
    private async Task<LoginResponse?> TokenAsync(Dictionary<string, string> form, CancellationToken cancellationToken)
    {
        form["client_id"] = ClientId;
        form["client_secret"] = ClientSecret;

        var client = httpClientFactory.CreateClient(KeycloakClient);
        using var response = await client.PostAsync(
            $"{Authority}/protocol/openid-connect/token", new FormUrlEncodedContent(form), cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return null; // unknown/disabled badge or expired refresh token → 401 at the endpoint
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
