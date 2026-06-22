using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Warehouse.Logistics.Core.Domain;
using Warehouse.SharedKernel.ValueObjects;

namespace Warehouse.Logistics.Core.Infrastructure.Persistence.Configurations;

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

/// <summary>InboundDelivery aggregate (ASN → put-away) with its receipt lines and optional dock slot.</summary>
internal sealed class InboundDeliveryConfiguration : IEntityTypeConfiguration<InboundDelivery>
{
    public void Configure(EntityTypeBuilder<InboundDelivery> builder)
    {
        builder.ToTable("inbound_deliveries");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).HasConversion(id => id.Value, v => new DeliveryId(v)).HasColumnName("id");
        builder.Property(d => d.Supplier).HasConversion(r => r.Value, v => new PartyRoleRef(v)).HasColumnName("supplier_role_id");
        builder.Property(d => d.Warehouse).HasConversion(w => w.Code, v => WarehouseRef.Of(v)).HasColumnName("warehouse_code").HasMaxLength(10);
        builder.Property(d => d.PlannedAt).HasColumnName("planned_at");
        builder.Property(d => d.Status).HasConversion<string>().HasColumnName("status").HasMaxLength(20);

        builder.OwnsOne(d => d.Slot, s =>
        {
            s.Property(x => x.DockCode).HasColumnName("slot_dock_code").HasMaxLength(20);
            s.Property(x => x.From).HasColumnName("slot_from");
            s.Property(x => x.To).HasColumnName("slot_to");
        });

        builder.OwnsMany(d => d.Lines, l =>
        {
            l.ToTable("inbound_delivery_lines");
            l.WithOwner().HasForeignKey("delivery_id");
            l.HasKey("delivery_id", nameof(DeliveryLine.LineNo));
            l.Property(x => x.LineNo).HasColumnName("line_no").ValueGeneratedNever();
            l.Property(x => x.Product).HasConversion(p => p.Value, v => ProductCode.Of(v)).HasColumnName("product_code").HasMaxLength(64);
            l.Property(x => x.Discrepancy).HasConversion<string>().HasColumnName("discrepancy").HasMaxLength(16);
            l.Property(x => x.Note).HasColumnName("note").HasMaxLength(512);

            l.OwnsOne(x => x.Expected, q => QuantityMap.Configure(q, "expected"));
            l.Navigation(x => x.Expected).IsRequired();
            l.OwnsOne(x => x.Actual, q => QuantityMap.Configure(q, "actual"));

            l.OwnsOne(x => x.Pack, p =>
            {
                p.Property(x => x.Unit).HasConversion(u => u.Code, c => UnitOfMeasure.FromCode(c)).HasColumnName("pack_unit").HasMaxLength(8);
                p.Property(x => x.FactorToBase).HasColumnName("pack_factor").HasPrecision(18, 4);
            });

            l.OwnsOne(x => x.Batch, b =>
            {
                b.Property(x => x.Number).HasColumnName("batch_number").HasMaxLength(32);
                b.Property(x => x.ExpiryDate).HasColumnName("batch_expiry");
            });
        });

        builder.Property<uint>("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();
    }
}

/// <summary>OutboundOrder aggregate (order → dispatch) with its order lines and ship-to address.</summary>
internal sealed class OutboundOrderConfiguration : IEntityTypeConfiguration<OutboundOrder>
{
    public void Configure(EntityTypeBuilder<OutboundOrder> builder)
    {
        builder.ToTable("outbound_orders");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).HasConversion(id => id.Value, v => new OrderId(v)).HasColumnName("id");
        builder.Property(o => o.Customer).HasConversion(r => r.Value, v => new PartyRoleRef(v)).HasColumnName("customer_role_id");
        builder.Property(o => o.Warehouse).HasConversion(w => w.Code, v => WarehouseRef.Of(v)).HasColumnName("warehouse_code").HasMaxLength(10);
        builder.Property(o => o.RequiredAt).HasColumnName("required_at");
        builder.Property(o => o.Status).HasConversion<string>().HasColumnName("status").HasMaxLength(20);

        builder.OwnsOne(o => o.ShipTo, a =>
        {
            a.Property(x => x.Street).HasColumnName("ship_to_street").HasMaxLength(200);
            a.Property(x => x.City).HasColumnName("ship_to_city").HasMaxLength(100);
            a.Property(x => x.PostalCode).HasColumnName("ship_to_postal_code").HasMaxLength(16);
            a.Property(x => x.CountryCode).HasColumnName("ship_to_country").HasMaxLength(2);
        });
        builder.Navigation(o => o.ShipTo).IsRequired();

        builder.OwnsMany(o => o.Lines, l =>
        {
            l.ToTable("outbound_order_lines");
            l.WithOwner().HasForeignKey("order_id");
            l.HasKey("order_id", nameof(OrderLine.LineNo));
            l.Property(x => x.LineNo).HasColumnName("line_no").ValueGeneratedNever();
            l.Property(x => x.Product).HasConversion(p => p.Value, v => ProductCode.Of(v)).HasColumnName("product_code").HasMaxLength(64);
            l.OwnsOne(x => x.Ordered, q => QuantityMap.Configure(q, "ordered"));
            l.Navigation(x => x.Ordered).IsRequired();
        });

        builder.Property<uint>("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();
    }
}

/// <summary>PickList aggregate: sequenced picking tasks for one order.</summary>
internal sealed class PickListConfiguration : IEntityTypeConfiguration<PickList>
{
    public void Configure(EntityTypeBuilder<PickList> builder)
    {
        builder.ToTable("pick_lists");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasConversion(id => id.Value, v => new PickListId(v)).HasColumnName("id");
        builder.Property(p => p.OrderId).HasConversion(id => id.Value, v => new OrderId(v)).HasColumnName("order_id");
        builder.HasIndex(p => p.OrderId);
        builder.Ignore(p => p.IsCompleted);

        builder.OwnsMany(p => p.Tasks, t =>
        {
            t.ToTable("pick_tasks");
            t.WithOwner().HasForeignKey("pick_list_id");
            t.HasKey("pick_list_id", nameof(PickTask.Sequence));
            t.Property(x => x.Sequence).HasColumnName("sequence").ValueGeneratedNever();
            t.Property(x => x.Location).HasConversion(l => l.Code, v => LocationRef.Of(v)).HasColumnName("location_code").HasMaxLength(60);
            t.Property(x => x.Product).HasConversion(pr => pr.Value, v => ProductCode.Of(v)).HasColumnName("product_code").HasMaxLength(64);
            t.Property(x => x.Status).HasConversion<string>().HasColumnName("status").HasMaxLength(16);
            t.Property(x => x.HandledBy).HasColumnName("handled_by").HasMaxLength(128);
            t.OwnsOne(x => x.Quantity, q => QuantityMap.Configure(q, "qty"));
            t.Navigation(x => x.Quantity).IsRequired();
            t.OwnsOne(x => x.Batch, b =>
            {
                b.Property(x => x.Number).HasColumnName("batch_number").HasMaxLength(32);
                b.Property(x => x.ExpiryDate).HasColumnName("batch_expiry");
            });
        });

        builder.Property<uint>("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();
    }
}

/// <summary>Shipment aggregate (packing → dispatch) with its packages.</summary>
internal sealed class ShipmentConfiguration : IEntityTypeConfiguration<Shipment>
{
    public void Configure(EntityTypeBuilder<Shipment> builder)
    {
        builder.ToTable("shipments");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasConversion(id => id.Value, v => new ShipmentId(v)).HasColumnName("id");
        builder.Property(s => s.OrderId).HasConversion(id => id.Value, v => new OrderId(v)).HasColumnName("order_id");
        builder.HasIndex(s => s.OrderId);
        builder.Property(s => s.Carrier).HasConversion(r => r.Value, v => new PartyRoleRef(v)).HasColumnName("carrier_role_id");
        builder.Property(s => s.Tracking)
            .HasConversion(t => t!.Value, v => TrackingNumber.Of(v))
            .HasColumnName("tracking_number").HasMaxLength(64);
        builder.Property(s => s.Status).HasConversion<string>().HasColumnName("status").HasMaxLength(16);
        builder.Property(s => s.DispatchedAt).HasColumnName("dispatched_at");

        builder.OwnsMany(s => s.Packages, p =>
        {
            p.ToTable("packages");
            p.WithOwner().HasForeignKey("shipment_id");
            p.HasKey("shipment_id", nameof(Package.Number));
            p.Property(x => x.Number).HasColumnName("number").ValueGeneratedNever();
            p.Property(x => x.Weight)
                .HasConversion(w => w.Kilograms, d => Weight.FromKilograms(d))
                .HasColumnName("weight_kg").HasPrecision(12, 3);
            p.Property(x => x.Description).HasColumnName("description").HasMaxLength(256);
            p.OwnsOne(x => x.Dimensions, dim =>
            {
                dim.Property(d => d.LengthCm).HasColumnName("length_cm").HasPrecision(10, 2);
                dim.Property(d => d.WidthCm).HasColumnName("width_cm").HasPrecision(10, 2);
                dim.Property(d => d.HeightCm).HasColumnName("height_cm").HasPrecision(10, 2);
            });
            p.Navigation(x => x.Dimensions).IsRequired();
        });

        builder.Property<uint>("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();
    }
}
