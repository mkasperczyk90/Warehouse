using Warehouse.ServiceDefaults.BiExport;

namespace Warehouse.IdentityBridge;

/// <summary>
/// Maps a Keycloak user (+ its realm roles) onto the <c>BronzeUser</c> shape from docs/BI_Plan.md
/// §5.4: <c>(user_id, display_name, role, home_warehouse_code, active)</c>.
/// </summary>
public static class BronzeUserMapper
{
    private const string SourceSystem = "identity-service";

    public static BronzeEvent Map(KeycloakUser user, IReadOnlyList<string> roles, DateTimeOffset polledAt)
    {
        var userId = user.Username ?? user.Id;
        var displayName = string.Join(
            " ", new[] { user.FirstName, user.LastName }.Where(part => !string.IsNullOrWhiteSpace(part)));
        var homeWarehouse = user.Attributes is { } attributes && attributes.TryGetValue("default_warehouse", out var values) && values.Count > 0
            ? values[0]
            : null;

        return new BronzeEvent(
            SourceSystem,
            EventType: "UserSnapshot",
            // Keyed by user id (not a random id): within the same UTC day, re-polls overwrite the
            // same object — only the latest snapshot per day survives, which is the correct grain
            // for a periodic dimension snapshot. Different days accumulate as separate history for
            // Silver's SCD2 handling (docs/BI_Plan.md §6), the same way the date partition already
            // works for every other Bronze writer in this project.
            EventId: userId,
            polledAt,
            Payload: new Dictionary<string, string?>
            {
                ["user_id"] = userId,
                ["display_name"] = displayName.Length > 0 ? displayName : null,
                ["role"] = roles.Count > 0 ? string.Join(",", roles) : null,
                ["home_warehouse_code"] = homeWarehouse,
                ["active"] = user.Enabled.ToString(),
            });
    }
}
