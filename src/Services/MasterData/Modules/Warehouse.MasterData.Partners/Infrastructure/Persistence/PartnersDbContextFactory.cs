using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Warehouse.MasterData.Partners.Infrastructure.Persistence;

/// <summary>Design-time factory for <c>dotnet ef</c> (model build + migrations only).</summary>
internal sealed class PartnersDbContextFactory : IDesignTimeDbContextFactory<PartnersDbContext>
{
    public PartnersDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PartnersDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=masterdata;Username=postgres;Password=postgres")
            .Options;
        return new PartnersDbContext(options);
    }
}
