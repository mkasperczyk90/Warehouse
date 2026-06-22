using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Warehouse.MasterData.Partners.Domain;

namespace Warehouse.MasterData.Partners.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps the <see cref="Party"/> aggregate root: identity + contact (owned), a unique tax id, and
/// the role collection (a TPH hierarchy in its own table — see <c>PartyRoleConfiguration</c>).
/// </summary>
internal sealed class PartyConfiguration : IEntityTypeConfiguration<Party>
{
    public void Configure(EntityTypeBuilder<Party> builder)
    {
        builder.ToTable("parties");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id)
            .HasConversion(id => id.Value, value => new PartyId(value))
            .HasColumnName("id");

        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();

        builder.Property(p => p.TaxId)
            .HasConversion(tax => tax.Value, value => TaxId.Of(value))
            .HasColumnName("tax_id")
            .HasMaxLength(15);
        builder.HasIndex(p => p.TaxId).IsUnique();

        builder.OwnsOne(p => p.Contact, c =>
        {
            c.Property(x => x.Email).HasColumnName("contact_email").HasMaxLength(256);
            c.Property(x => x.Phone).HasColumnName("contact_phone").HasMaxLength(32);
        });
        builder.Navigation(p => p.Contact).IsRequired();

        builder.HasMany(p => p.Roles)
            .WithOne()
            .HasForeignKey("PartyId")
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(p => p.Roles)
            .HasField("_roles")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // PostgreSQL system column xmin as the optimistic-concurrency token.
        builder.Property<uint>("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();
    }
}
