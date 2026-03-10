using Warehouse.SharedKernel.EventMessages;

namespace Warehouse.Product.Infrastructure.EventRegistration;

public static class EventMessageConfiguration
{
	public static readonly IReadOnlyList<MessagePublisherBinding> PublisherBindings = new List<MessagePublisherBinding>();

	public static readonly IReadOnlyList<MessageListingBinding> ListingBindings = new List<MessageListingBinding>
	{
		new MessageListingBinding("product-inventory-updates", "product-inventory-events"),
	};
}
