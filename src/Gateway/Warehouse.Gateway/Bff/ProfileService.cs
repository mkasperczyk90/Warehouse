using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using Warehouse.Gateway.Auth;

namespace Warehouse.Gateway.Bff;

/// <summary>
/// The desk user's own profile (admin Profile screen). Identity — badge, name, role, email — and the
/// seed preferences come straight from the validated Keycloak token; the editable preferences (phone,
/// default warehouse, language) are overlaid from an in-memory store so a save round-trips within the
/// session. This keeps the gateway BFF self-contained (no profile database, no Keycloak admin client),
/// which suits the dev / ephemeral deployment. To persist across restarts, swap the overlay for writes
/// to the Keycloak Admin API. The desk only ever reads/writes its OWN profile: the route id must equal
/// the token subject, otherwise the lookup returns null (→ 404 at the endpoint).
/// </summary>
public sealed class ProfileService
{
    private static readonly string[] Languages = ["en", "pl"];
    private readonly ConcurrentDictionary<string, ProfilePrefsDto> _overlay = new(StringComparer.Ordinal);

    /// <summary>The caller's profile, or null when <paramref name="requestedId"/> is not the caller's own id.</summary>
    public ProfileDto? Build(string accessToken, string requestedId)
    {
        var user = AuthClaims.ToUser(accessToken);
        if (!string.Equals(user.Id, requestedId, StringComparison.Ordinal))
        {
            return null;
        }

        var payload = AuthClaims.Payload(accessToken);
        var prefs = _overlay.GetValueOrDefault(user.Id);
        var lastLogin = LastLoginOf(payload);

        return new ProfileDto(
            user.Id,
            user.Badge,
            user.Name,
            user.Role,
            user.Email,
            prefs?.Phone ?? ClaimString(payload, "phone"),
            prefs?.DefaultWarehouseId ?? user.DefaultWarehouseId,
            prefs?.Language ?? user.Language,
            lastLogin,
            [new ProfileSessionDto("current", lastLogin, "Desk · this session")]);
    }

    /// <summary>Applies a preferences edit and returns the refreshed profile, or null for a foreign id.</summary>
    public ProfileDto? Update(string accessToken, string requestedId, ProfilePrefsDto prefs)
    {
        var user = AuthClaims.ToUser(accessToken);
        if (!string.Equals(user.Id, requestedId, StringComparison.Ordinal))
        {
            return null;
        }

        var language = Languages.Contains(prefs.Language) ? prefs.Language : user.Language;
        _overlay[user.Id] = prefs with { Language = language };
        return Build(accessToken, requestedId);
    }

    private static string ClaimString(JsonElement payload, string name) =>
        payload.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString()! : "";

    /// <summary>The token's auth time (or issued-at) formatted like the mock's "yyyy-MM-dd HH:mm".</summary>
    private static string LastLoginOf(JsonElement payload)
    {
        var epoch = ClaimNumber(payload, "auth_time") ?? ClaimNumber(payload, "iat");
        var when = epoch is { } e ? DateTimeOffset.FromUnixTimeSeconds(e).UtcDateTime : DateTime.UtcNow;
        return when.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
    }

    private static long? ClaimNumber(JsonElement payload, string name) =>
        payload.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetInt64() : null;
}
