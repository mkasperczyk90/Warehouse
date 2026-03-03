using Microsoft.EntityFrameworkCore;

namespace Warehouse.Inventory.Infrastructure.Persistence;

public class InventoryDbContext: DbContext
{
	public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options)
	{
	}

	public DbSet<Domain.Entities.Inventory> Inventory => Set<Domain.Entities.Inventory>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		modelBuilder.ApplyConfigurationsFromAssembly(typeof(InventoryDbContext).Assembly);
	}
}
