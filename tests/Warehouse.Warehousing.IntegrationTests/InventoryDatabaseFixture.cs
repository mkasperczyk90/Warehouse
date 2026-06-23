using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Respawn;
using Respawn.Graph;
using Testcontainers.PostgreSql;
using Warehouse.Warehousing.Inventory.Infrastructure;
using Warehouse.Warehousing.Inventory.Infrastructure.Persistence;
using Xunit;

namespace Warehouse.Warehousing.IntegrationTests;

/// <summary>
/// One PostgreSQL container shared by the whole collection: started once, migrated once, then wiped
/// between tests with Respawn (data only). Resolves the real Inventory repositories + ledger through
/// the production DI wiring, so tests exercise the actual EF Core mappings and SQL.
/// </summary>
public sealed class InventoryDatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine").Build();

    private Respawner _respawner = null!;

    public ServiceProvider Services { get; private set; } = null!;

    private string ConnectionString => _container.GetConnectionString();

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();

        Services = new ServiceCollection()
            .AddDbContext<InventoryDbContext>(options => options
                .UseNpgsql(ConnectionString)
                // Migration history is what production runs; the model-differ's pending-changes guard
                // is advisory here (model drift is caught by the dedicated migrations CI check).
                .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning)))
            .AddInventoryRepositories()
            .BuildServiceProvider();

        await using (var scope = Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
            await db.Database.MigrateAsync();
        }

        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["inventory"],
            TablesToIgnore = [new Table("inventory", "__EFMigrationsHistory")],
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

    /// <summary>A DI scope whose repositories, ledger and unit of work share one DbContext.</summary>
    public AsyncServiceScope NewScope() => Services.CreateAsyncScope();
}

[CollectionDefinition(Name)]
public sealed class InventoryDatabaseCollectionDefinition : ICollectionFixture<InventoryDatabaseFixture>
{
    public const string Name = "inventory-db";
}

/// <summary>Base for the database tests: joins the shared-container collection and resets the data
/// before each test so they stay independent.</summary>
[Collection(InventoryDatabaseCollectionDefinition.Name)]
public abstract class InventoryIntegrationTest(InventoryDatabaseFixture fixture) : IAsyncLifetime
{
    protected InventoryDatabaseFixture Fixture { get; } = fixture;

    public async ValueTask InitializeAsync() => await Fixture.ResetAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
