using Asp.Versioning;
using Microsoft.EntityFrameworkCore;
using Warehouse.Product.Api.Extensions;
using Warehouse.Product.Api.Settings;
using Warehouse.Product.Infrastructure;
using Warehouse.Product.Infrastructure.Persistence;
using Warehouse.Product.Infrastructure.Settings;
using Warehouse.SharedKernel.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddSingleton(TimeProvider.System);

var keycloakOptions = builder.Configuration
	.GetSection(KeyCloakOptions.SectionName)
	.Get<KeyCloakOptions>()!; // move to AddKeyCloakAuth

var rabbitMqSettings = builder.Configuration
	.GetSection(RabbitMqSettings.SectionName)
	.Get<RabbitMqSettings>()!;

builder.Services.AddControllers();

builder.AddInfrastructure();

builder.AddOpenTelemetryLogging();

builder.Services.AddOpenApi();

builder.Services.AddKeyCloakAuth(keycloakOptions);

builder.Services.AddAuthorization();

builder.Services.AddHealthChecks()
	.AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!)
	.AddRabbitMQ(
		sp => new RabbitMQ.Client.ConnectionFactory()
		{
			Uri = new Uri($"amqp://{rabbitMqSettings.UserName}:{rabbitMqSettings.Password}@{rabbitMqSettings.HostName}:5672/")
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
	var context = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
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

namespace Warehouse.Product.Api
{
	public partial class Program { }
}
