using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Warehouse.SharedKernel.ValueObjects;
using Warehouse.Warehousing.Topology.Domain;

namespace Warehouse.Warehousing.Topology.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps the <see cref="WarehouseSite"/> aggregate. Rooms, locations and docks are entities that
/// live inside the aggregate, so they're mapped as owned collections (their identity is the
/// warehouse code plus their own code) — the warehouse is the only entry point.
/// </summary>
internal sealed class WarehouseSiteConfiguration : IEntityTypeConfiguration<WarehouseSite>
{
    public void Configure(EntityTypeBuilder<WarehouseSite> builder)
    {
        builder.ToTable("warehouses");

        builder.HasKey(w => w.Id);
        builder.Property(w => w.Id)
            .HasConversion(code => code.Value, value => WarehouseCode.Of(value))
            .HasColumnName("code")
            .HasMaxLength(10);

        builder.Property(w => w.Name).HasMaxLength(200).IsRequired();

        builder.OwnsOne(w => w.Address, a =>
        {
            a.Property(x => x.Street).HasColumnName("street").HasMaxLength(200);
            a.Property(x => x.City).HasColumnName("city").HasMaxLength(120);
            a.Property(x => x.PostalCode).HasColumnName("postal_code").HasMaxLength(20);
            a.Property(x => x.CountryCode).HasColumnName("country_code").HasMaxLength(2);
        });
        builder.Navigation(w => w.Address).IsRequired();

        builder.OwnsMany(w => w.Rooms, room =>
        {
            room.ToTable("rooms");
            room.WithOwner().HasForeignKey("warehouse_code");
            room.Property(r => r.Id)
                .HasConversion(code => code.Value, value => RoomCode.Of(value))
                .HasColumnName("code")
                .HasMaxLength(10);
            room.HasKey("warehouse_code", "Id");
            room.Property(r => r.Type).HasConversion<string>().HasColumnName("type").HasMaxLength(16);

            room.OwnsOne(r => r.Environment, env =>
            {
                env.Property(e => e.HumidityControlled).HasColumnName("humidity_controlled");
                env.OwnsOne(e => e.MaintainedTemperature, t =>
                {
                    t.Property(x => x.MinCelsius).HasColumnName("temp_min_c").HasPrecision(6, 2);
                    t.Property(x => x.MaxCelsius).HasColumnName("temp_max_c").HasPrecision(6, 2);
                });
                env.Navigation(e => e.MaintainedTemperature).IsRequired();
            });
            room.Navigation(r => r.Environment).IsRequired();

            room.OwnsMany(r => r.Locations, loc =>
            {
                loc.ToTable("locations");
                loc.WithOwner().HasForeignKey("warehouse_code", "room_code");
                loc.Property(l => l.Id)
                    .HasConversion(code => code.Value, value => LocationCode.Of(value))
                    .HasColumnName("code")
                    .HasMaxLength(60);
                loc.HasKey("warehouse_code", "room_code", "Id");
                loc.Property(l => l.Kind).HasConversion<string>().HasColumnName("kind").HasMaxLength(16);
                loc.Property(l => l.Capacity)
                    .HasConversion(v => v.CubicMeters, d => Volume.FromCubicMeters(d))
                    .HasColumnName("capacity_m3").HasPrecision(12, 4);
                loc.Property(l => l.MaxLoad)
                    .HasConversion(w => w.Kilograms, d => Weight.FromKilograms(d))
                    .HasColumnName("max_load_kg").HasPrecision(12, 3);
            });
        });

        builder.OwnsMany(w => w.Docks, dock =>
        {
            dock.ToTable("docks");
            dock.WithOwner().HasForeignKey("warehouse_code");
            dock.Property(d => d.Id)
                .HasConversion(code => code.Value, value => DockCode.Of(value))
                .HasColumnName("code")
                .HasMaxLength(10);
            dock.HasKey("warehouse_code", "Id");
            dock.Property(d => d.Direction).HasConversion<string>().HasColumnName("direction").HasMaxLength(16);
        });

        builder.Property<uint>("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();
    }
}
