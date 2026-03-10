using Wolverine;
using Wolverine.RabbitMQ;

namespace Warehouse.SharedKernel.EventMessages;

public class RabbitEventMessageExtension: IWolverineExtension
{
	private readonly IEnumerable<MessagePublisherBinding> _publishers;
	private readonly IEnumerable<MessageListingBinding> _listeners;

	public RabbitEventMessageExtension(
		IEnumerable<MessagePublisherBinding> publishers,
		IEnumerable<MessageListingBinding> listeners)
	{
		_publishers = publishers;
		_listeners = listeners;
	}

	public void Configure(WolverineOptions options)
	{
		foreach (var binding in _publishers)
		{
			options.PublishMessage(binding.MessageType)
				.ToRabbitExchange(binding.Exchange);
		}

		foreach (var binding in _listeners)
		{
			options.ListenToRabbitQueue(binding.Queue).ConfigureQueue(cx =>
			{
				cx.BindExchange(binding.Exchange);
			});
		}
	}
}
