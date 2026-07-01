using System.Text;
using System.Text.Json;
using Warehouse.ServiceDefaults;

namespace Warehouse.Gateway.Auth;

/// <summary>
/// Reads the desk-user view out of a Keycloak access token's claims (no signature check here — the token
/// is validated by the JwtBearer middleware on every subsequent call; this only shapes the login response).
/// Pure, so it is unit-testable without Keycloak running.
/// </summary>
internal static class AuthClaims
{
    public static CurrentUserDto ToUser(string accessToken)
    {
        var payload = DecodePayload(accessToken);

        string Claim(string name) =>
            payload.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString()! : "";

        var name = Claim("name");
        if (name.Length == 0)
        {
            name = Claim("preferred_username");
        }

        var language = Claim("language");
        if (language.Length == 0)
        {
            language = "en";
        }

        return new CurrentUserDto(
            Claim("sub"),
            Claim("badge"),
            name,
            ExtractRole(payload),
            Claim("email"),
            Claim("default_warehouse"),
            language);
    }

    /// <summary>The raw token payload, for callers that need a claim beyond the desk-user view
    /// (e.g. the profile's phone and last-login time). Same no-signature-check caveat as <see cref="ToUser"/>.</summary>
    internal static JsonElement Payload(string accessToken) => DecodePayload(accessToken);

    /// <summary>The caller's app role — the first realm role that is one of the app's known roles, desk or
    /// terminal (Keycloak also adds default roles like <c>offline_access</c>, which we ignore).</summary>
    private static string ExtractRole(JsonElement payload)
    {
        if (payload.TryGetProperty("realm_access", out var realmAccess) &&
            realmAccess.TryGetProperty("roles", out var roles) &&
            roles.ValueKind == JsonValueKind.Array)
        {
            foreach (var role in roles.EnumerateArray())
            {
                if (role.GetString() is { } value && AppRoles.All.Contains(value))
                {
                    return value;
                }
            }
        }

        return "";
    }

    private static JsonElement DecodePayload(string jwt)
    {
        var parts = jwt.Split('.');
        if (parts.Length < 2)
        {
            throw new FormatException("Access token is not a JWT.");
        }

        var json = Encoding.UTF8.GetString(Base64UrlDecode(parts[1]));
        return JsonDocument.Parse(json).RootElement.Clone();
    }

    private static byte[] Base64UrlDecode(string value)
    {
        var s = value.Replace('-', '+').Replace('_', '/');
        s += (s.Length % 4) switch { 2 => "==", 3 => "=", _ => "" };
        return Convert.FromBase64String(s);
    }
}
