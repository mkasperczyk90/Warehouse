using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Warehouse.Inventory.Domain.Entities;

namespace Warehouse.Inventory.Infrastructure.Persistence.Configurations;

public class InventoryConfiguration: IEntityTypeConfiguration<Inventory.Domain.Entities.Inventory>
{
	public void Configure(EntityTypeBuilder<Inventory.Domain.Entities.Inventory> builder)
	{
		builder.ToTable("Inventory");

		var converterProductId = new ValueConverter<ProductId, Guid>(
			id => id.Value,
			guid => new (guid));

		var converterInventoryId = new ValueConverter<InventoryId, Guid>(
			id => id.Value,
			guid => new (guid));

		builder.HasKey(x => x.Id);
		builder.Property(x => x.Id)
			.HasConversion(converterInventoryId);

		builder.Property(x => x.ProductId)
			.HasConversion(converterProductId)
			.IsRequired();

		builder.Property(x => x.AddedAt)
			.IsRequired();

		builder.Property(x => x.AddedBy)
			.IsRequired();
	}
}
