using Microsoft.EntityFrameworkCore;

namespace Warehouse.Inventory.Infrastructure.Persistence;

public class InventoryDbContext: DbContext
{
	public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options)
	{
	}

	public DbSet<Domain.Inventory> Inventory => Set<Domain.Inventory>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		modelBuilder.ApplyConfigurationsFromAssembly(typeof(InventoryDbContext).Assembly);
	}
}
