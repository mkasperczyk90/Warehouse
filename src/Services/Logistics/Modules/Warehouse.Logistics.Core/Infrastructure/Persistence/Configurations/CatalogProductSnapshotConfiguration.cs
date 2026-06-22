using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Warehouse.Logistics.Core.Domain;
using Warehouse.Logistics.Core.Domain.Replicas;

namespace Warehouse.Logistics.Core.Infrastructure.Persistence.Configurations;

/// <summary>Local Catalog replica — keyed by product code, projected from <c>ProductDefined</c> events.</summary>
internal sealed class CatalogProductSnapshotConfiguration : IEntityTypeConfiguration<CatalogProductSnapshot>
{
    public void Configure(EntityTypeBuilder<CatalogProductSnapshot> builder)
    {
        builder.ToTable("catalog_product_snapshots");
        builder.HasKey(p => p.Code);
        builder.Property(p => p.Code)
            .HasConversion(c => c.Value, v => ProductCode.Of(v))
            .HasColumnName("product_code").HasMaxLength(64);
        builder.Property(p => p.BaseUnit).HasColumnName("base_unit").HasMaxLength(8);
        builder.Property(p => p.IsBatchTracked).HasColumnName("is_batch_tracked");
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at");
    }
}
