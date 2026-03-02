using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Warehouse.Inventory.Infrastructure.Persistence.Configurations;

public class InventoryConfiguration: IEntityTypeConfiguration<Inventory.Domain.Inventory>
{
	public void Configure(EntityTypeBuilder<Inventory.Domain.Inventory> builder)
	{
		builder.ToTable("Inventory");

		builder.HasKey(x => x.Id);
		builder.Property(x => x.ProductId)
			.IsRequired();

		builder.Property(x => x.AddedAt)
			.IsRequired();

		builder.Property(x => x.AddedBy)
			.IsRequired();
	}
}
