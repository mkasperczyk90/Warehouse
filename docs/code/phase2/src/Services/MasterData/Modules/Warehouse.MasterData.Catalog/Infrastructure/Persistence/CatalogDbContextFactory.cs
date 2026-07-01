using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Warehouse.MasterData.Catalog.Infrastructure.Persistence;

/// <summary>
/// Design-time factory used only by <c>dotnet ef</c> to build the model and scaffold migrations.
/// The connection string is never opened for <c>migrations add</c>; it just has to be well-formed.
/// At runtime the context is configured by the host (Aspire), not here.
/// </summary>
internal sealed class CatalogDbContextFactory : IDesignTimeDbContextFactory<CatalogDbContext>
{
    public CatalogDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=masterdata;Username=postgres;Password=postgres")
            .Options;
        return new CatalogDbContext(options);
    }
}
