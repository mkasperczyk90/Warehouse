namespace Warehouse.Product.Domain.ProcessedEvent.Entities;

// TODO: Wolverine did it after setting it
public class ProcessedEvent
{
	public Guid EventId { get; private set; }
	public DateTime ProcessedAt { get; private set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
	private ProcessedEvent() { }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

	public ProcessedEvent(Guid eventId)
	{
		EventId = eventId;
		ProcessedAt = DateTime.UtcNow;
	}
}
