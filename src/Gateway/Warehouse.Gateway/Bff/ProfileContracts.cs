namespace Warehouse.Gateway.Bff;

/// <summary>A past sign-in shown on the profile's activity panel (mirrors the admin's <c>ProfileSession</c>).</summary>
public sealed record ProfileSessionDto(string Id, string When, string Device);

/// <summary>The full desk-user record behind the avatar — identity from the token plus editable prefs
/// (mirrors the admin's <c>UserProfile</c>).</summary>
public sealed record ProfileDto(
    string Id,
    string Badge,
    string Name,
    string Role,
    string Email,
    string Phone,
    string DefaultWarehouseId,
    string Language,
    string LastLogin,
    IReadOnlyList<ProfileSessionDto> RecentSessions);

/// <summary>The fields the profile screen lets the user change (mirrors the admin's <c>ProfilePrefs</c>).</summary>
public sealed record ProfilePrefsDto(string Phone, string DefaultWarehouseId, string Language);
