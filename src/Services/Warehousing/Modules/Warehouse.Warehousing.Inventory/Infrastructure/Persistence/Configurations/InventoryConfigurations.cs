using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Warehouse.SharedKernel.ValueObjects;
using Warehouse.Warehousing.Inventory.Domain;
using Warehouse.Warehousing.Inventory.Domain.Replicas;

namespace Warehouse.Warehousing.Inventory.Infrastructure.Persistence.Configurations;

/// <summary>Shared mapping for the ubiquitous <see cref="Quantity"/> value object (amount + unit),
/// reused everywhere a quantity is persisted.</summary>
internal static class QuantityMap
{
    public static void Configure<TOwner>(OwnedNavigationBuilder<TOwner, Quantity> q, string prefix)
        where TOwner : class
    {
        q.Property(x => x.Amount).HasColumnName($"{prefix}_amount").HasPrecision(18, 3);
        q.Property(x => x.Unit)
            .HasConversion(unit => unit.Code, code => UnitOfMeasure.FromCode(code))
            .HasColumnName($"{prefix}_unit").HasMaxLength(8);
    }
}

/// <summary>StockItem aggregate: a SKU+batch at a location, with its hard allocations.</summary>
internal sealed class StockItemConfiguration : IEntityTypeConfiguration<StockItem>
{
    public void Configure(EntityTypeBuilder<StockItem> builder)
    {
        builder.ToTable("stock_items");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasConversion(id => id.Value, v => new StockItemId(v)).HasColumnName("id");
        builder.Property(s => s.Sku).HasConversion(x => x.Value, v => Sku.Of(v)).HasColumnName("sku").HasMaxLength(32);
        builder.Property(s => s.Batch).HasConversion(x => x!.Value, v => BatchNumber.Of(v)).HasColumnName("batch").HasMaxLength(32);
        builder.Property(s => s.Location).HasConversion(x => x.Value, v => LocationCode.Of(v)).HasColumnName("location").HasMaxLength(60);
        builder.Property(s => s.Status).HasConversion<string>().HasColumnName("status").HasMaxLength(16);

        builder.OwnsOne(s => s.OnHand, q => QuantityMap.Configure(q, "on_hand"));
        builder.Navigation(s => s.OnHand).IsRequired();
        builder.OwnsOne(s => s.Allocated, q => QuantityMap.Configure(q, "allocated"));
        builder.Navigation(s => s.Allocated).IsRequired();
        builder.Ignore(s => s.Available);

        builder.HasIndex(s => new { s.Sku, s.Batch, s.Location }).IsUnique();

        builder.OwnsMany(s => s.Allocations, a =>
        {
            a.ToTable("allocations");
            a.WithOwner().HasForeignKey("stock_item_id");
            a.Property(x => x.Id).HasConversion(id => id.Value, v => new AllocationId(v)).HasColumnName("id");
            a.HasKey("stock_item_id", "Id");
            a.Property(x => x.ReservationId).HasConversion(id => id.Value, v => new StockReservationId(v)).HasColumnName("reservation_id");
            a.Property(x => x.OrderRef).HasConversion(x => x.Value, v => OrderRef.Of(v)).HasColumnName("order_ref").HasMaxLength(64);
            a.Property(x => x.Status).HasConversion<string>().HasColumnName("status").HasMaxLength(16);
            a.OwnsOne(x => x.Quantity, q => QuantityMap.Configure(q, "qty"));
            a.Navigation(x => x.Quantity).IsRequired();
        });

        builder.Property<uint>("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();
    }
}

/// <summary>Batch aggregate: expiry + QC hold, unique per SKU+number.</summary>
internal sealed class BatchConfiguration : IEntityTypeConfiguration<Batch>
{
    public void Configure(EntityTypeBuilder<Batch> builder)
    {
        builder.ToTable("batches");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).HasConversion(id => id.Value, v => new BatchId(v)).HasColumnName("id");
        builder.Property(b => b.Sku).HasConversion(x => x.Value, v => Sku.Of(v)).HasColumnName("sku").HasMaxLength(32);
        builder.Property(b => b.Number).HasConversion(x => x.Value, v => BatchNumber.Of(v)).HasColumnName("number").HasMaxLength(32);
        builder.Property(b => b.ExpiryDate).HasColumnName("expiry_date");
        builder.Property(b => b.SupplierRef).HasColumnName("supplier_ref").HasMaxLength(128);
        builder.Property(b => b.Quality).HasConversion<string>().HasColumnName("quality").HasMaxLength(16);
        builder.HasIndex(b => new { b.Sku, b.Number }).IsUnique();
        builder.Property<uint>("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();
    }
}

/// <summary>Soft reservation aggregate (SKU-level, per warehouse).</summary>
internal sealed class StockReservationConfiguration : IEntityTypeConfiguration<StockReservation>
{
    public void Configure(EntityTypeBuilder<StockReservation> builder)
    {
        builder.ToTable("stock_reservations");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasConversion(id => id.Value, v => new StockReservationId(v)).HasColumnName("id");
        builder.Property(r => r.Sku).HasConversion(x => x.Value, v => Sku.Of(v)).HasColumnName("sku").HasMaxLength(32);
        builder.Property(r => r.Warehouse).HasConversion(x => x.Value, v => WarehouseCode.Of(v)).HasColumnName("warehouse").HasMaxLength(10);
        builder.Property(r => r.OrderRef).HasConversion(x => x.Value, v => OrderRef.Of(v)).HasColumnName("order_ref").HasMaxLength(64);
        builder.Property(r => r.Status).HasConversion<string>().HasColumnName("status").HasMaxLength(20);
        builder.OwnsOne(r => r.Quantity, q => QuantityMap.Configure(q, "qty"));
        builder.Navigation(r => r.Quantity).IsRequired();
        builder.OwnsOne(r => r.Allocated, q => QuantityMap.Configure(q, "allocated"));
        builder.Navigation(r => r.Allocated).IsRequired();
        builder.Ignore(r => r.Outstanding);
        builder.HasIndex(r => r.OrderRef);
        builder.Property<uint>("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();
    }
}

/// <summary>The append-only stock ledger. No concurrency token: rows are never updated.</summary>
internal sealed class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("stock_movements");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasConversion(id => id.Value, v => new MovementId(v)).HasColumnName("id");
        builder.Property(m => m.Type).HasConversion<string>().HasColumnName("type").HasMaxLength(24);
        builder.Property(m => m.Sku).HasConversion(x => x.Value, v => Sku.Of(v)).HasColumnName("sku").HasMaxLength(32);
        builder.Property(m => m.Batch).HasConversion(x => x!.Value, v => BatchNumber.Of(v)).HasColumnName("batch").HasMaxLength(32);
        builder.Property(m => m.From).HasConversion(x => x!.Value, v => LocationCode.Of(v)).HasColumnName("from_location").HasMaxLength(60);
        builder.Property(m => m.To).HasConversion(x => x!.Value, v => LocationCode.Of(v)).HasColumnName("to_location").HasMaxLength(60);
        builder.Property(m => m.PerformedBy).HasColumnName("performed_by").HasMaxLength(128).IsRequired();
        builder.Property(m => m.Reason).HasColumnName("reason").HasMaxLength(256);
        builder.Property(m => m.OccurredAt).HasColumnName("occurred_at");
        builder.OwnsOne(m => m.Quantity, q => QuantityMap.Configure(q, "qty"));
        builder.Navigation(m => m.Quantity).IsRequired();
        builder.HasIndex(m => new { m.Sku, m.OccurredAt });
    }
}

/// <summary>Handling unit (pallet/carton) aggregate with its content lines.</summary>
internal sealed class HandlingUnitConfiguration : IEntityTypeConfiguration<HandlingUnit>
{
    public void Configure(EntityTypeBuilder<HandlingUnit> builder)
    {
        builder.ToTable("handling_units");
        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id).HasConversion(id => id.Value, v => new HandlingUnitId(v)).HasColumnName("id");
        builder.Property(h => h.Lpn).HasConversion(x => x.Value, v => LpnCode.Of(v)).HasColumnName("lpn").HasMaxLength(20);
        builder.HasIndex(h => h.Lpn).IsUnique();
        builder.Property(h => h.Kind).HasConversion<string>().HasColumnName("kind").HasMaxLength(16);
        builder.Property(h => h.Location).HasConversion(x => x.Value, v => LocationCode.Of(v)).HasColumnName("location").HasMaxLength(60);
        builder.Property(h => h.Status).HasConversion<string>().HasColumnName("status").HasMaxLength(16);
        builder.Ignore(h => h.IsEmpty);

        builder.OwnsMany(h => h.Lines, l =>
        {
            l.ToTable("handling_unit_lines");
            l.WithOwner().HasForeignKey("handling_unit_id");
            l.Property(x => x.Sku).HasConversion(x => x.Value, v => Sku.Of(v)).HasColumnName("sku").HasMaxLength(32);
            l.Property(x => x.Batch).HasConversion(x => x!.Value, v => BatchNumber.Of(v)).HasColumnName("batch").HasMaxLength(32);
            l.OwnsOne(x => x.Quantity, q => QuantityMap.Configure(q, "qty"));
            l.Navigation(x => x.Quantity).IsRequired();
        });

        builder.Property<uint>("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();
    }
}

/// <summary>Stocktake aggregate: a blind count over a set of locations, with its recorded count lines.</summary>
internal sealed class StocktakeConfiguration : IEntityTypeConfiguration<Stocktake>
{
    public void Configure(EntityTypeBuilder<Stocktake> builder)
    {
        builder.ToTable("stocktakes");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasConversion(id => id.Value, v => new StocktakeId(v)).HasColumnName("id");
        builder.Property(s => s.Label).HasColumnName("label").HasMaxLength(120).IsRequired();
        builder.Property(s => s.OrderedBy).HasColumnName("ordered_by").HasMaxLength(128).IsRequired();
        builder.Property(s => s.Status).HasConversion<string>().HasColumnName("status").HasMaxLength(16);
        builder.Property(s => s.StartedAt).HasColumnName("started_at");
        builder.Property(s => s.ApprovedAt).HasColumnName("approved_at");

        // Scope is a small set of location codes; persisted as a single joined column (no child table).
        builder.Property(s => s.Scope)
            .HasConversion(
                v => string.Join(',', v.Select(c => c.Value)),
                s => s.Length == 0
                    ? new List<LocationCode>()
                    : s.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(LocationCode.Of).ToList(),
                new ValueComparer<IReadOnlyCollection<LocationCode>>(
                    (a, b) => a!.SequenceEqual(b!),
                    v => v.Aggregate(0, (h, c) => HashCode.Combine(h, c.GetHashCode())),
                    v => v.ToList()))
            .HasColumnName("scope")
            .HasMaxLength(2000);

        builder.OwnsMany(s => s.Lines, l =>
        {
            l.ToTable("stocktake_count_lines");
            l.WithOwner().HasForeignKey("stocktake_id");
            l.Property(x => x.Location).HasConversion(x => x.Value, v => LocationCode.Of(v)).HasColumnName("location").HasMaxLength(60);
            l.Property(x => x.Sku).HasConversion(x => x.Value, v => Sku.Of(v)).HasColumnName("sku").HasMaxLength(32);
            l.Property(x => x.Batch).HasConversion(x => x!.Value, v => BatchNumber.Of(v)).HasColumnName("batch").HasMaxLength(32);
            l.Property(x => x.CountedBy).HasColumnName("counted_by").HasMaxLength(128).IsRequired();
            l.OwnsOne(x => x.Counted, q => QuantityMap.Configure(q, "counted"));
            l.Navigation(x => x.Counted).IsRequired();
            l.OwnsOne(x => x.Expected, q => QuantityMap.Configure(q, "expected"));
            l.Navigation(x => x.Expected).IsRequired();
            l.Ignore(x => x.HasDifference);
        });

        builder.Property<uint>("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();
    }
}

/// <summary>Local replica of the Catalog product card (updated by integration events).</summary>
internal sealed class ProductSnapshotConfiguration : IEntityTypeConfiguration<ProductSnapshot>
{
    public void Configure(EntityTypeBuilder<ProductSnapshot> builder)
    {
        builder.ToTable("product_snapshots");
        builder.HasKey(p => p.Sku);
        builder.Property(p => p.Sku).HasConversion(x => x.Value, v => Sku.Of(v)).HasColumnName("sku").HasMaxLength(32);
        builder.Property(p => p.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(p => p.BaseUnit).HasConversion(u => u.Code, c => UnitOfMeasure.FromCode(c)).HasColumnName("base_unit").HasMaxLength(8);
        builder.Property(p => p.UnitWeight).HasConversion(w => w.Kilograms, d => Weight.FromKilograms(d)).HasColumnName("unit_weight_kg").HasPrecision(12, 3);
        builder.Property(p => p.UnitVolume).HasConversion(v => v.CubicMeters, d => Volume.FromCubicMeters(d)).HasColumnName("unit_volume_m3").HasPrecision(12, 4);
        builder.Property(p => p.RequiresColdChain).HasColumnName("requires_cold_chain");
        builder.Property(p => p.IsHazardous).HasColumnName("is_hazardous");
        builder.Property(p => p.IsBatchTracked).HasColumnName("is_batch_tracked");
        builder.Property(p => p.HasExpiryDate).HasColumnName("has_expiry_date");
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at");
        builder.OwnsOne(p => p.RequiredTemperature, t =>
        {
            t.Property(x => x.MinCelsius).HasColumnName("temp_min_c").HasPrecision(6, 2);
            t.Property(x => x.MaxCelsius).HasColumnName("temp_max_c").HasPrecision(6, 2);
        });
    }
}

/// <summary>Local replica of a Topology location (updated by integration events).</summary>
internal sealed class LocationSnapshotConfiguration : IEntityTypeConfiguration<LocationSnapshot>
{
    public void Configure(EntityTypeBuilder<LocationSnapshot> builder)
    {
        builder.ToTable("location_snapshots");
        builder.HasKey(l => l.Code);
        builder.Property(l => l.Code).HasConversion(x => x.Value, v => LocationCode.Of(v)).HasColumnName("code").HasMaxLength(60);
        builder.Property(l => l.Warehouse).HasConversion(x => x.Value, v => WarehouseCode.Of(v)).HasColumnName("warehouse").HasMaxLength(10);
        builder.Property(l => l.Room).HasColumnName("room").HasMaxLength(10);
        builder.HasIndex(l => new { l.Warehouse, l.Room });
        builder.Property(l => l.IsHazmatZone).HasColumnName("is_hazmat_zone");
        builder.Property(l => l.Capacity).HasConversion(v => v.CubicMeters, d => Volume.FromCubicMeters(d)).HasColumnName("capacity_m3").HasPrecision(12, 4);
        builder.Property(l => l.MaxLoad).HasConversion(w => w.Kilograms, d => Weight.FromKilograms(d)).HasColumnName("max_load_kg").HasPrecision(12, 3);
        builder.Property(l => l.UpdatedAt).HasColumnName("updated_at");
        builder.OwnsOne(l => l.EnvironmentTemperature, t =>
        {
            t.Property(x => x.MinCelsius).HasColumnName("temp_min_c").HasPrecision(6, 2);
            t.Property(x => x.MaxCelsius).HasColumnName("temp_max_c").HasPrecision(6, 2);
        });
        builder.Navigation(l => l.EnvironmentTemperature).IsRequired();
    }
}
