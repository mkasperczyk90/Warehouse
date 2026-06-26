using Warehouse.Gateway.Auth;
using Warehouse.Gateway.Bff;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Identity: validate the Keycloak JWTs (issued by the 'keycloak' resource, realm 'warehouse'). Token
// validation lives here at the edge; downstream services trust the gateway (blog #11 architecture). The
// authority resolves through Aspire service discovery (the JwtBearer backchannel inherits ServiceDefaults'
// handlers). Issuer validation is off in dev because Keycloak's advertised issuer differs from the internal
// service-discovery host — pin a ValidIssuer once a stable public URL is in front.
builder.Services.AddAuthentication(Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Keycloak:Authority"] ?? "http://keycloak/realms/warehouse";
        options.RequireHttpsMetadata = false;
        options.Audience = "account";
        options.TokenValidationParameters.ValidateIssuer = false;
    });
builder.Services.AddAuthorization();

// Badge sign-in broker → Keycloak token endpoint (keeps the confidential client secret server-side).
builder.Services.AddHttpClient(AuthBroker.KeycloakClient, c => c.BaseAddress = new Uri("http://keycloak/"));
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
builder.Services.AddScoped<SearchService>();

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

// Work-queue landing — "what needs me now" aggregated across Inventory + Logistics (admin-10).
app.MapGet("/api/worklist", async (HttpRequest request, WorklistService worklist, CancellationToken ct) =>
{
    var warehouseId = request.Headers["X-Warehouse-Id"].FirstOrDefault();
    return Results.Ok(await worklist.BuildAsync(warehouseId, ct));
}).RequireAuthorization();

// Global search — "where is X" across products, stock, inbound, orders and locations.
app.MapGet("/api/search", async (string? q, HttpRequest request, SearchService search, CancellationToken ct) =>
{
    var warehouseId = request.Headers["X-Warehouse-Id"].FirstOrDefault();
    return Results.Ok(await search.SearchAsync(q ?? string.Empty, warehouseId, ct));
}).RequireAuthorization();

// Everything proxied to the services requires a valid token (the desk is authenticated at the edge).
app.MapReverseProxy().RequireAuthorization();

app.Run();
