namespace Warehouse.ServiceDefaults;

/// <summary>
/// The realm roles the app knows about, grouped by hub, plus the authorization policy names built from
/// them. The desk (admin panel) roles run the office screens; the terminal roles are the handheld floor
/// operators. Shared by the gateway (edge routing) and every backend service (zero-trust floor), so they
/// all agree on the same set — this mirrors the realm roles in <c>src/Identity/realms/warehouse-realm.json</c>.
/// </summary>
public static class AppRoles
{
    public const string Manager = "manager";
    public const string Coordinator = "coordinator";
    public const string Inspector = "inspector";
    public const string Operator = "operator";
    public const string Forklift = "forklift";

    /// <summary>Admin-panel (desk) roles.</summary>
    public static readonly string[] Desk = [Manager, Coordinator, Inspector];

    /// <summary>Handheld terminal (floor) roles.</summary>
    public static readonly string[] Terminal = [Operator, Forklift];

    /// <summary>Every warehouse staff role — either hub.</summary>
    public static readonly string[] All = [Manager, Coordinator, Inspector, Operator, Forklift];

    /// <summary>Desk-only screens (worklist, search, catalog, topology, dispatch).</summary>
    public const string DeskPolicy = "Desk";

    /// <summary>Handheld-only endpoints (the terminal task hub).</summary>
    public const string TerminalPolicy = "Terminal";

    /// <summary>Shared services both hubs use (inventory, logistics) and the caller's own profile.</summary>
    public const string StaffPolicy = "Staff";
}
