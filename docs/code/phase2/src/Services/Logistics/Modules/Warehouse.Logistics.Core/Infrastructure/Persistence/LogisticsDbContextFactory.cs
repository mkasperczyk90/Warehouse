using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Warehouse.Logistics.Core.Infrastructure.Persistence;

/// <summary>
/// Design-time factory used only by <c>dotnet ef</c> to build the model and scaffold migrations.
/// The connection string is never opened for <c>migrations add</c>; it just has to be well-formed.
/// At runtime the context is configured by the host (Aspire), not here.
/// </summary>
internal sealed class LogisticsDbContextFactory : IDesignTimeDbContextFactory<LogisticsDbContext>
{
    public LogisticsDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<LogisticsDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=logistics;Username=postgres;Password=postgres")
            .Options;
        return new LogisticsDbContext(options);
    }
}
