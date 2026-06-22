using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Warehouse.Warehousing.Inventory.Infrastructure.Persistence;

/// <summary>
/// Design-time factory used only by <c>dotnet ef</c> to build the model and scaffold migrations.
/// Inventory shares the Warehousing service's <c>warehouse</c> database with Topology (separate
/// schemas). The connection string is never opened for <c>migrations add</c>; it just has to be
/// well-formed. At runtime the context is configured by the host (Aspire), not here.
/// </summary>
internal sealed class InventoryDbContextFactory : IDesignTimeDbContextFactory<InventoryDbContext>
{
    public InventoryDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=warehouse;Username=postgres;Password=postgres")
            .Options;
        return new InventoryDbContext(options);
    }
}
