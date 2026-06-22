using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Warehouse.Warehousing.Topology.Infrastructure.Persistence;

/// <summary>Design-time factory for <c>dotnet ef</c> (model build + migrations only).</summary>
internal sealed class TopologyDbContextFactory : IDesignTimeDbContextFactory<TopologyDbContext>
{
    public TopologyDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<TopologyDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=warehouse;Username=postgres;Password=postgres")
            .Options;
        return new TopologyDbContext(options);
    }
}
