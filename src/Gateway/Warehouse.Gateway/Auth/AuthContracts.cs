namespace Warehouse.Gateway.Auth;

/// <summary>The badge the desk scanned at sign-in.</summary>
public sealed record LoginRequest(string Badge);

/// <summary>What the broker returns to the SPA: the bearer token (+ refresh) and the desk user derived
/// from its claims, so the admin's existing <c>CurrentUser</c> shape is preserved.</summary>
public sealed record LoginResponse(string AccessToken, string? RefreshToken, int ExpiresIn, CurrentUserDto User);

public sealed record CurrentUserDto(
    string Id,
    string Badge,
    string Name,
    string Role,
    string Email,
    string DefaultWarehouseId,
    string Language);
