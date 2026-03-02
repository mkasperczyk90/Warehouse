using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Warehouse.Product.Domain.ProcessedEvent.Entities;

namespace Warehouse.Product.Infrastructure.Persistence.Configurations;

public class ProcessedEventConfiguration: IEntityTypeConfiguration<ProcessedEvent>
{
	public void Configure(EntityTypeBuilder<ProcessedEvent> builder)
	{
		builder.ToTable("ProcessedEvents")
			.HasKey(e => e.EventId);

		builder.Property(x => x.ProcessedAt)
			.IsRequired();
	}
}
