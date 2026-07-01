using Warehouse.Gateway.Auth;
using Warehouse.Gateway.Bff;
using Warehouse.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Identity: validate the Keycloak JWTs (signature + issuer + audience) and register the Desk/Terminal/Staff
// role policies — shared wiring in ServiceDefaults so the gateway and the backend services agree. No
// fallback policy here: the gateway gates explicitly (per-endpoint below + per-route in appsettings.json),
// and /api/auth/login stays anonymous. The authority is the resolved Keycloak URL the AppHost injects; the
// badge broker mints tokens from that same authority, so issuer + signature validation line up.
builder.AddWarehouseJwtAuth();

// The BFF fan-out forwards the caller's bearer to the services (which now validate it themselves), so it
// needs the current request's HttpContext.
builder.Services.AddHttpContextAccessor();

// Badge sign-in broker → Keycloak token endpoint (keeps the confidential client secret server-side). The
// broker posts to the same Keycloak__Authority the JwtBearer validates against (absolute URL), so the token
// it mints and the token the gateway validates share one issuer.
builder.Services.AddHttpClient(AuthBroker.KeycloakClient);
builder.Services.AddScoped<AuthBroker>();

// YARP reverse proxy. Routes/clusters come from configuration; cluster destinations are logical
// service names (e.g. http://logistics-api) resolved at runtime by Aspire service discovery.
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddServiceDiscoveryDestinationResolver();

// BFF: a few cross-context read aggregations the admin needs (the gateway is the only place that may
// fan out across services). The clients resolve by Aspire service discovery + inherit the standard
// resilience handler from ServiceDefaults. Paths the FE owns (/api/worklist, …) are not YARP routes,
// so they fall through to these minimal-API endpoints.
builder.Services.AddHttpClient(BffClients.Warehousing, c => c.BaseAddress = new Uri("http://warehousing-api/"));
builder.Services.AddHttpClient(BffClients.Logistics, c => c.BaseAddress = new Uri("http://logistics-api/"));
builder.Services.AddHttpClient(BffClients.MasterData, c => c.BaseAddress = new Uri("http://masterdata-api/"));
builder.Services.AddScoped<BffFetch>();
builder.Services.AddScoped<WorklistService>();
builder.Services.AddScoped<TerminalTasksService>();
builder.Services.AddScoped<SearchService>();
// Profile is derived from the caller's token + an in-memory prefs overlay, so it is a singleton (the
// overlay must outlive a single request). No downstream call, hence no scoped HttpClient.
builder.Services.AddSingleton<ProfileService>();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapDefaultEndpoints();

// Badge sign-in — anonymous (it is how you get a token). Returns the bearer token + the desk user.
app.MapPost("/api/auth/login", async (LoginRequest request, AuthBroker broker, CancellationToken ct) =>
{
    var result = await broker.LoginAsync(request.Badge, ct);
    return result is null ? Results.Unauthorized() : Results.Ok(result);
}).AllowAnonymous();

// Silent renew — anonymous (the refresh token is the credential). The api seam calls this when a call
// 401s on an expired access token, then retries; a rejected refresh token (expired/revoked) → 401.
app.MapPost("/api/auth/refresh", async (RefreshRequest request, AuthBroker broker, CancellationToken ct) =>
{
    var result = await broker.RefreshAsync(request.RefreshToken, ct);
    return result is null ? Results.Unauthorized() : Results.Ok(result);
}).AllowAnonymous();

// Sign-out — ends the Keycloak session (revokes the refresh token) so it can't be renewed after logout.
// Anonymous and best-effort: possessing the refresh token authorises revoking it.
app.MapPost("/api/auth/logout", async (RefreshRequest request, AuthBroker broker, CancellationToken ct) =>
{
    await broker.LogoutAsync(request.RefreshToken, ct);
    return Results.NoContent();
}).AllowAnonymous();

// Work-queue landing — "what needs me now" aggregated across Inventory + Logistics (admin-10).
app.MapGet("/api/worklist", async (HttpRequest request, WorklistService worklist, CancellationToken ct) =>
{
    var warehouseId = request.Headers["X-Warehouse-Id"].FirstOrDefault();
    return Results.Ok(await worklist.BuildAsync(warehouseId, ct));
}).RequireAuthorization(AppRoles.DeskPolicy);

// Terminal Task hub — the handheld operator's open work, aggregated across Inventory + Logistics and
// scoped to the operator's warehouse (the terminal sends it as X-Warehouse-Id at the api seam).
app.MapGet("/api/terminal/tasks", async (HttpRequest request, TerminalTasksService tasks, CancellationToken ct) =>
{
    var warehouseId = request.Headers["X-Warehouse-Id"].FirstOrDefault();
    return Results.Ok(await tasks.BuildAsync(warehouseId, ct));
}).RequireAuthorization(AppRoles.TerminalPolicy);

// Global search — "where is X" across products, stock, inbound, orders and locations.
app.MapGet("/api/search", async (string? q, HttpRequest request, SearchService search, CancellationToken ct) =>
{
    var warehouseId = request.Headers["X-Warehouse-Id"].FirstOrDefault();
    return Results.Ok(await search.SearchAsync(q ?? string.Empty, warehouseId, ct));
}).RequireAuthorization(AppRoles.DeskPolicy);

// Desk profile — identity from the token + editable prefs (admin Profile screen). The desk reads and
// writes only its OWN profile, so the route id must match the token subject (else 404).
app.MapGet("/api/profile/{id}", (string id, HttpContext http, ProfileService profiles) =>
{
    var profile = profiles.Build(BearerToken(http), id);
    return profile is null ? Results.NotFound() : Results.Ok(profile);
}).RequireAuthorization(AppRoles.StaffPolicy);

app.MapPost("/api/profile/{id}", (string id, ProfilePrefsDto prefs, HttpContext http, ProfileService profiles) =>
{
    var profile = profiles.Update(BearerToken(http), id, prefs);
    return profile is null ? Results.NotFound() : Results.Ok(profile);
}).RequireAuthorization(AppRoles.StaffPolicy);

// Everything proxied to the services requires a valid token (baseline floor); each route additionally
// pins a role policy via its "AuthorizationPolicy" in appsettings.json — shared services (inventory,
// logistics) allow either hub (Staff), desk-only services (catalog, topology, dispatch) require Desk.
app.MapReverseProxy().RequireAuthorization();

app.Run();

// The validated bearer token, reused to shape the caller's profile (RequireAuthorization guarantees it).
static string BearerToken(HttpContext http)
{
    var header = http.Request.Headers.Authorization.ToString();
    return header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
        ? header["Bearer ".Length..]
        : header;
}
