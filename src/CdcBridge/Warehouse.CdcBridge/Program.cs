using Amazon;
using Amazon.S3;
using Warehouse.CdcBridge;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.Services.Configure<CdcBridgeOptions>(builder.Configuration.GetSection(CdcBridgeOptions.SectionName));
var options = builder.Configuration.GetSection(CdcBridgeOptions.SectionName).Get<CdcBridgeOptions>() ?? new CdcBridgeOptions();

builder.Services.AddSingleton<IAmazonS3>(_ => new AmazonS3Client(RegionEndpoint.GetBySystemName(options.Region)));
builder.Services.AddSingleton<CdcRecordWriter>();
builder.Services.AddHostedService<CdcListenerService>();

var host = builder.Build();
await host.RunAsync();
