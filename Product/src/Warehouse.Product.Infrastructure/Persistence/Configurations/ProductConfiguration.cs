using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Warehouse.Product.Domain.Products.Entities;
using DomainProduct = Warehouse.Product.Domain.Products.Entities.Product;
namespace Warehouse.Product.Infrastructure.Persistence.Configurations;

public class ProductConfiguration: IEntityTypeConfiguration<DomainProduct>
{
	public void Configure(EntityTypeBuilder<DomainProduct> builder)
	{
		builder.ToTable("Products")
			.HasIndex(x => x.Name)
			.IsUnique();

		var converter = new ValueConverter<ProductId, Guid>(
			id => id.Value,
			guid => new (guid));

		builder.HasKey(x => x.Id);

		builder.Property(x => x.Id)
			.HasConversion(converter);

		builder.Property(x => x.Name)
			.IsRequired()
			.HasMaxLength(200);

		builder.Property(x => x.Description)
			.IsRequired()
			.HasMaxLength(1000);

		builder.Property(x => x.Price)
			.HasPrecision(18, 2);

		builder.Property(x => x.Amount)
			.IsRequired();

		builder.Property(x => x.CreatedAt)
			.IsRequired();

		builder.Property(x => x.UpdatedAt);
	}
}
