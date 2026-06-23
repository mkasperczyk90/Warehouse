using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Respawn;
using Respawn.Graph;
using Testcontainers.PostgreSql;
using Warehouse.Logistics.Core.Infrastructure;
using Warehouse.Logistics.Core.Infrastructure.Persistence;
using Xunit;

namespace Warehouse.Logistics.IntegrationTests;

/// <summary>
/// One PostgreSQL container shared by the whole collection: started once, migrated once, then wiped
/// between tests with Respawn (data only — schema and migration history survive). Resolves the real
/// repositories through the production DI wiring (<see cref="LogisticsInfrastructure"/>), so tests
/// exercise the actual EF Core mappings and SQL, not a fake.
/// </summary>
public sealed class LogisticsDatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine").Build();

    private Respawner _respawner = null!;

    public ServiceProvider Services { get; private set; } = null!;

    private string ConnectionString => _container.GetConnectionString();

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();

        Services = new ServiceCollection()
            .AddDbContext<LogisticsDbContext>(options => options
                .UseNpgsql(ConnectionString)
                // The migration history is what production runs; the model-differ's pending-changes
                // guard is advisory here (model drift is caught by the dedicated migrations CI check).
                .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning)))
            .AddLogisticsRepositories()
            .BuildServiceProvider();

        await using (var scope = Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LogisticsDbContext>();
            await db.Database.MigrateAsync();
        }

        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["logistics"],
            TablesToIgnore = [new Table("logistics", "__EFMigrationsHistory")],
        });
    }

    public async ValueTask DisposeAsync()
    {
        if (Services is not null)
        {
            await Services.DisposeAsync();
        }

        await _container.DisposeAsync();
    }

    /// <summary>Wipe all data so each test starts from an empty (but migrated) database.</summary>
    public async Task ResetAsync()
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await _respawner.ResetAsync(connection);
    }

    /// <summary>A DI scope whose repositories and unit of work share one DbContext (one transaction).</summary>
    public AsyncServiceScope NewScope() => Services.CreateAsyncScope();
}

[CollectionDefinition(Name)]
public sealed class LogisticsDatabaseCollectionDefinition : ICollectionFixture<LogisticsDatabaseFixture>
{
    public const string Name = "logistics-db";
}

/// <summary>Base for the database tests: joins the shared-container collection and resets the data
/// before each test so they stay independent.</summary>
[Collection(LogisticsDatabaseCollectionDefinition.Name)]
public abstract class LogisticsIntegrationTest(LogisticsDatabaseFixture fixture) : IAsyncLifetime
{
    protected LogisticsDatabaseFixture Fixture { get; } = fixture;

    public async ValueTask InitializeAsync() => await Fixture.ResetAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
