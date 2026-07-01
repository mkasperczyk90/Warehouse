using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;

namespace Warehouse.ServiceDefaults;

/// <summary>
/// Keycloak nests realm roles under a <c>realm_access.roles</c> JSON claim, which JwtBearer does NOT map to
/// role claims — so <c>RequireRole</c> / role policies would never match. This flattens the app's known
/// realm roles into standard role claims, which is what the Desk / Terminal / Staff policies check.
/// Idempotent: it skips roles it has already added, so it is safe if the pipeline runs it more than once.
/// </summary>
public sealed class KeycloakRolesClaimsTransformation : IClaimsTransformation
{
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity identity ||
            principal.FindFirst("realm_access")?.Value is not { Length: > 0 } realmAccess)
        {
            return Task.FromResult(principal);
        }

        try
        {
            using var doc = JsonDocument.Parse(realmAccess);
            if (doc.RootElement.TryGetProperty("roles", out var roles) && roles.ValueKind == JsonValueKind.Array)
            {
                foreach (var role in roles.EnumerateArray())
                {
                    if (role.GetString() is { } value && AppRoles.All.Contains(value) &&
                        !identity.HasClaim(identity.RoleClaimType, value))
                    {
                        identity.AddClaim(new Claim(identity.RoleClaimType, value));
                    }
                }
            }
        }
        catch (JsonException)
        {
            // Malformed realm_access — treat as carrying no roles.
        }

        return Task.FromResult(principal);
    }
}
