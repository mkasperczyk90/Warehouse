namespace Warehouse.IdentityBridge;

/// <summary>
/// Bound from the <c>"IdentityBridge"</c> configuration section. Keycloak has no persistent
/// database in this stack (dev-mode/embedded storage — see docs/BI_Plan.md's Identity source note),
/// so it can't be reached by the CDC pipeline; this worker polls its Admin REST API instead.
/// </summary>
public sealed class IdentityBridgeOptions
{
    public const string SectionName = "IdentityBridge";

    /// <summary>Same BI lake bucket the other Bronze writers use.</summary>
    public string BucketName { get; init; } = "";

    public string Region { get; init; } = "eu-central-1";

    public string KeycloakBaseUrl { get; init; } = "http://localhost:8080";

    public string Realm { get; init; } = "warehouse";

    /// <summary>
    /// Dev bootstrap admin (master realm, admin-cli client) — this environment's Keycloak has no
    /// dedicated service-account client (docs/deploy.md already documents the dev bootstrap admin
    /// as an accepted simplification for this ephemeral environment; harden before any other use).
    /// </summary>
    public string AdminUsername { get; init; } = "admin";

    public string AdminPassword { get; init; } = "admin";

    public int PollIntervalSeconds { get; init; } = 300;
}
