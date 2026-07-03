using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;

namespace Warehouse.IdentityBridge;

/// <summary>
/// Thin wrapper over Keycloak's Admin REST API — the only source for user/role data in this stack,
/// since Keycloak runs in dev-mode with no persistent database the CDC pipeline could tap into
/// (docs/BI_Plan.md, Identity source note). Auth via the master realm's built-in <c>admin-cli</c>
/// client and the dev bootstrap admin credentials — see <see cref="IdentityBridgeOptions"/>.
/// </summary>
public sealed class KeycloakAdminClient(HttpClient httpClient, IOptions<IdentityBridgeOptions> options)
{
    private readonly IdentityBridgeOptions _options = options.Value;

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = "admin-cli",
            ["username"] = _options.AdminUsername,
            ["password"] = _options.AdminPassword,
        };

        using var response = await httpClient.PostAsync(
            $"{_options.KeycloakBaseUrl}/realms/master/protocol/openid-connect/token",
            new FormUrlEncodedContent(form),
            cancellationToken);
        response.EnsureSuccessStatusCode();

        var token = await response.Content.ReadFromJsonAsync<KeycloakTokenResponse>(cancellationToken: cancellationToken);
        return token?.AccessToken
            ?? throw new InvalidOperationException("Keycloak token response had no access_token.");
    }

    public async Task<IReadOnlyList<KeycloakUser>> ListUsersAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get, $"{_options.KeycloakBaseUrl}/admin/realms/{_options.Realm}/users?max=1000");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var users = await response.Content.ReadFromJsonAsync<List<KeycloakUser>>(cancellationToken: cancellationToken);
        return users ?? [];
    }

    /// <summary><c>UserRepresentation</c> doesn't include roles by itself — a separate call per user is required.</summary>
    public async Task<IReadOnlyList<string>> GetRealmRolesAsync(
        string accessToken, string userId, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get, $"{_options.KeycloakBaseUrl}/admin/realms/{_options.Realm}/users/{userId}/role-mappings/realm");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var roles = await response.Content.ReadFromJsonAsync<List<KeycloakRole>>(cancellationToken: cancellationToken);
        return roles?.Select(r => r.Name).ToList() ?? [];
    }
}
