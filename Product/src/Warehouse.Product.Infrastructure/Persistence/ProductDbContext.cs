using Microsoft.EntityFrameworkCore;
using Warehouse.Product.Domain.ProcessedEvent.Entities;
using DomainProduct =  Warehouse.Product.Domain.Products.Entities.Product;

namespace Warehouse.Product.Infrastructure.Persistence;

public class ProductDbContext: DbContext
{
	public ProductDbContext(DbContextOptions<ProductDbContext> options) : base(options)
	{
	}

	public DbSet<DomainProduct> Products => Set<DomainProduct>();
	public DbSet<ProcessedEvent> ProcessedEvents => Set<ProcessedEvent>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProductDbContext).Assembly);
	}
}
