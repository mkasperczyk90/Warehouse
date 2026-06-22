using Microsoft.EntityFrameworkCore;
using Warehouse.MasterData.Partners.Domain;
using Warehouse.SharedKernel.Application;

namespace Warehouse.MasterData.Partners.Infrastructure.Persistence;

/// <summary>
/// EF Core unit of work for the Partners context. Owns the <c>partners</c> schema inside the
/// MasterData service's PostgreSQL database (alongside the Catalog context's <c>catalog</c> schema).
/// </summary>
public sealed class PartnersDbContext(DbContextOptions<PartnersDbContext> options)
    : DbContext(options), IUnitOfWork
{
    /// <summary>The schema this context owns inside the shared MasterData database.</summary>
    public const string Schema = "partners";

    public DbSet<Party> Parties => Set<Party>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PartnersDbContext).Assembly);
    }
}
