using Asp.Versioning;
using Microsoft.EntityFrameworkCore;
using Warehouse.Inventory.Api.Extensions;
using Warehouse.Inventory.Api.Settings;
using Warehouse.Inventory.Application;
using Warehouse.Inventory.Infrastructure;
using Warehouse.Inventory.Infrastructure.Persistence;
using Warehouse.Inventory.Infrastructure.Settings;
using Warehouse.SharedKernel.Middleware;

var builder = WebApplication.CreateBuilder(args);

var keycloakOptions = builder.Configuration
	.GetSection(KeyCloakOptions.SectionName)
	.Get<KeyCloakOptions>()!; // move to AddKeyCloakAuth

var rabbitMqSettings = builder.Configuration
	.GetSection(RabbitMqSettings.SectionName)
	.Get<RabbitMqSettings>()!;

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.AddInfrastructure();
builder.Services.AddApplicationLayer();

builder.AddOpenTelemetryLogging();

builder.Services.AddKeyCloakAuth(keycloakOptions);
builder.Services.AddAuthorization();

builder.Services.AddHealthChecks()
	.AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!)
	.AddRabbitMQ(
		_ => new RabbitMQ.Client.ConnectionFactory()
		{
			Uri = new Uri($"amqp://{rabbitMqSettings.UserName}:{rabbitMqSettings.Password}@{rabbitMqSettings.HostName}:{rabbitMqSettings.Port}/")
		}.CreateConnectionAsync(),
		name: "rabbitmq"
	);

builder.Services.AddApiVersioning(options =>
	{
		options.AssumeDefaultVersionWhenUnspecified = true;
		options.DefaultApiVersion = new ApiVersion(1, 0);

		options.ReportApiVersions = true;

		options.ApiVersionReader = ApiVersionReader.Combine(
			new UrlSegmentApiVersionReader(),
			new HeaderApiVersionReader("x-api-version"),
			new MediaTypeApiVersionReader("x-api-version")
		);
	}).AddApiExplorer(options =>
	{
		options.GroupNameFormat = "'v'VVV";
		options.SubstituteApiVersionInUrl = true;
	});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
	app.MapOpenApi();

	using var scope = app.Services.CreateScope();
	var context = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
	context.Database.Migrate();
}

if (!app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Testing"))
{
	app.UseHttpsRedirection();
}
app.UseRequestContextLogging();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health");

app.Run();

namespace Warehouse.Inventory.Api
{
	public partial class Program { }
}
