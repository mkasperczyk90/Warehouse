using Microsoft.EntityFrameworkCore;
using Warehouse.Product.Domain.Interfaces;
using Warehouse.Product.Domain.ProcessedEvent.Entities;

namespace Warehouse.Product.Infrastructure.Persistence.ProcessedEventRepository;

public class ProcessedEventRepository(ProductDbContext context) : IProcessedEventRepository
{
	private readonly ProductDbContext _context = context ?? throw new ArgumentNullException(nameof(context));

	public async Task InsertAsync(ProcessedEvent processedEvent, CancellationToken ct = default) => await _context.ProcessedEvents.AddAsync(processedEvent, ct);

	public async Task<bool> Exists(Guid eventId, CancellationToken ct = default) =>
		await context.ProcessedEvents
			.AnyAsync(p => p.EventId == eventId, ct);
}
