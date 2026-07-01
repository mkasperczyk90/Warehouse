using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Warehouse.MasterData.Catalog.Domain;
using Warehouse.SharedKernel.ValueObjects;

namespace Warehouse.MasterData.Catalog.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps the <see cref="ProductType"/> aggregate: the SKU key and all value objects go to inline
/// columns (owned types / converters), unit conversions to a child table, and <c>xmin</c> is the
/// optimistic-concurrency token (no domain change — it's a PostgreSQL system column).
/// </summary>
internal sealed class ProductTypeConfiguration : IEntityTypeConfiguration<ProductType>
{
    public void Configure(EntityTypeBuilder<ProductType> builder)
    {
        builder.ToTable("product_types");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id)
            .HasConversion(sku => sku.Value, value => Sku.Of(value))
            .HasColumnName("sku")
            .HasMaxLength(32);

        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();

        builder.Property(p => p.Ean)
            .HasConversion(ean => ean!.Value, value => Ean.Of(value))
            .HasColumnName("ean")
            .HasMaxLength(13);
        builder.HasIndex(p => p.Ean).IsUnique();

        builder.Property(p => p.Category)
            .HasConversion<string>()
            .HasColumnName("category")
            .HasMaxLength(32);

        builder.Property(p => p.BaseUnit)
            .HasConversion(unit => unit.Code, code => UnitOfMeasure.FromCode(code))
            .HasColumnName("base_unit")
            .HasMaxLength(8);

        builder.Property(p => p.UnitWeight)
            .HasConversion(weight => weight.Kilograms, kg => Weight.FromKilograms(kg))
            .HasColumnName("unit_weight_kg")
            .HasPrecision(12, 3);

        builder.OwnsOne(p => p.Dimensions, d =>
        {
            d.Property(x => x.LengthCm).HasColumnName("length_cm").HasPrecision(10, 2);
            d.Property(x => x.WidthCm).HasColumnName("width_cm").HasPrecision(10, 2);
            d.Property(x => x.HeightCm).HasColumnName("height_cm").HasPrecision(10, 2);
        });
        builder.Navigation(p => p.Dimensions).IsRequired();

        builder.OwnsOne(p => p.Storage, s =>
        {
            s.Property(x => x.RequiresColdChain).HasColumnName("requires_cold_chain");
            s.Property(x => x.IsHazardous).HasColumnName("is_hazardous");
            s.OwnsOne(x => x.Temperature, t =>
            {
                t.Property(r => r.MinCelsius).HasColumnName("temp_min_c").HasPrecision(6, 2);
                t.Property(r => r.MaxCelsius).HasColumnName("temp_max_c").HasPrecision(6, 2);
            });
        });
        builder.Navigation(p => p.Storage).IsRequired();

        builder.Property(p => p.IsBatchTracked).HasColumnName("is_batch_tracked");
        builder.Property(p => p.HasExpiryDate).HasColumnName("has_expiry_date");

        builder.OwnsMany(p => p.UnitConversions, c =>
        {
            c.ToTable("product_unit_conversions");
            c.Property(x => x.Unit)
                .HasConversion(unit => unit.Code, code => UnitOfMeasure.FromCode(code))
                .HasColumnName("unit")
                .HasMaxLength(8);
            c.Property(x => x.FactorToBase).HasColumnName("factor_to_base").HasPrecision(18, 6);
        });

        // PostgreSQL system column xmin as the optimistic-concurrency token (no domain change).
        builder.Property<uint>("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();
    }
}
