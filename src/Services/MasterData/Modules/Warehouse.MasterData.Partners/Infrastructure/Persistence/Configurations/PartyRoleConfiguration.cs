using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Warehouse.MasterData.Partners.Domain;

namespace Warehouse.MasterData.Partners.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps the <see cref="PartyRole"/> hierarchy as Table-Per-Hierarchy: one <c>party_roles</c> table
/// with a <c>role_type</c> discriminator. Role-specific data (a customer's shipping addresses, a
/// carrier's service levels) is configured on the derived types.
/// </summary>
internal sealed class PartyRoleConfiguration : IEntityTypeConfiguration<PartyRole>
{
    public void Configure(EntityTypeBuilder<PartyRole> builder)
    {
        builder.ToTable("party_roles");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id)
            .HasConversion(id => id.Value, value => new PartyRoleId(value))
            .HasColumnName("id");

        builder.Property(r => r.Code).HasColumnName("code").HasMaxLength(32).IsRequired();

        builder.HasDiscriminator<string>("role_type")
            .HasValue<SupplierRole>("supplier")
            .HasValue<CustomerRole>("customer")
            .HasValue<CarrierRole>("carrier");
    }
}

/// <summary>A customer's shipping addresses are an owned collection in their own table.</summary>
internal sealed class CustomerRoleConfiguration : IEntityTypeConfiguration<CustomerRole>
{
    public void Configure(EntityTypeBuilder<CustomerRole> builder)
    {
        builder.OwnsMany(c => c.ShippingAddresses, a =>
        {
            a.ToTable("customer_shipping_addresses");
            a.Property(x => x.Street).HasColumnName("street").HasMaxLength(200);
            a.Property(x => x.City).HasColumnName("city").HasMaxLength(120);
            a.Property(x => x.PostalCode).HasColumnName("postal_code").HasMaxLength(20);
            a.Property(x => x.CountryCode).HasColumnName("country_code").HasMaxLength(2);
        });
        builder.Navigation(c => c.ShippingAddresses)
            .HasField("_shippingAddresses")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

/// <summary>A carrier's offered service levels are a primitive collection (enum values).</summary>
internal sealed class CarrierRoleConfiguration : IEntityTypeConfiguration<CarrierRole>
{
    public void Configure(EntityTypeBuilder<CarrierRole> builder)
    {
        builder.PrimitiveCollection(c => c.Services)
            .HasField("_services")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
