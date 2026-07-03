using System.Text.Json.Serialization;

namespace Warehouse.IdentityBridge;

/// <summary>A slice of Keycloak's <c>UserRepresentation</c> — only the fields BronzeUser needs.</summary>
public sealed record KeycloakUser(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("username")] string? Username,
    [property: JsonPropertyName("firstName")] string? FirstName,
    [property: JsonPropertyName("lastName")] string? LastName,
    [property: JsonPropertyName("enabled")] bool Enabled,
    [property: JsonPropertyName("attributes")] IReadOnlyDictionary<string, IReadOnlyList<string>>? Attributes);

/// <summary>A slice of Keycloak's <c>RoleRepresentation</c>, as returned by the
/// <c>/users/{id}/role-mappings/realm</c> endpoint.</summary>
public sealed record KeycloakRole([property: JsonPropertyName("name")] string Name);

internal sealed record KeycloakTokenResponse([property: JsonPropertyName("access_token")] string AccessToken);
