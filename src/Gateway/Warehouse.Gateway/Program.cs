using Warehouse.Gateway.Bff;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

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

app.MapDefaultEndpoints();

// Work-queue landing — "what needs me now" aggregated across Inventory + Logistics (admin-10).
app.MapGet("/api/worklist", async (HttpRequest request, WorklistService worklist, CancellationToken ct) =>
{
    var warehouseId = request.Headers["X-Warehouse-Id"].FirstOrDefault();
    return Results.Ok(await worklist.BuildAsync(warehouseId, ct));
});

// Global search — "where is X" across products, stock, inbound, orders and locations.
app.MapGet("/api/search", async (string? q, HttpRequest request, SearchService search, CancellationToken ct) =>
{
    var warehouseId = request.Headers["X-Warehouse-Id"].FirstOrDefault();
    return Results.Ok(await search.SearchAsync(q ?? string.Empty, warehouseId, ct));
});

app.MapReverseProxy();

app.Run();
