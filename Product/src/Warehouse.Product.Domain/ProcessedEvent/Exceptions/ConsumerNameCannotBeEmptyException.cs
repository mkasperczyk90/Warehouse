using Warehouse.SharedKernel.Exceptions;

namespace Warehouse.Product.Domain.ProcessedEvent.Exceptions;

public class ConsumerNameCannotBeEmptyException(string name)
	: DomainException($"Consumer name cannot be empty. Name: {name}")
{
	public override string Code => "consumer_name_cannot_be_empty";
}
