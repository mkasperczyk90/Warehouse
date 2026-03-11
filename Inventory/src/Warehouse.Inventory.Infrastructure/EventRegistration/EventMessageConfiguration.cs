using Warehouse.Contracts;
using Warehouse.BuildingBlocks.EventMessages;

namespace Warehouse.Inventory.Infrastructure.EventRegistration;

public static class EventMessageConfiguration
{
	public static readonly IReadOnlyList<MessagePublisherBinding> PublisherBindings = new List<MessagePublisherBinding>
	{
		new(typeof(ProductInventoryAddedEvent), "product-inventory-events"),
	};

	public static readonly IReadOnlyList<MessageListingBinding> ListingBindings = new List<MessageListingBinding>();
}
