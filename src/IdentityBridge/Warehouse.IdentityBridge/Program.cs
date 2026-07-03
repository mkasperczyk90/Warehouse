using Warehouse.IdentityBridge;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.AddBiExport();

builder.Services.Configure<IdentityBridgeOptions>(builder.Configuration.GetSection(IdentityBridgeOptions.SectionName));
builder.Services.AddHttpClient<KeycloakAdminClient>();
builder.Services.AddHostedService<UserSnapshotPollingService>();

var host = builder.Build();
await host.RunAsync();
