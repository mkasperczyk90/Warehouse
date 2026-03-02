using Warehouse.SharedKernel.Exceptions;

namespace Warehouse.Product.Domain.ValueObjects;

// TODO: Should be used in Product in the feature
public record Price
{
	public decimal Value { get; init; }
	public string Currency { get; init; }

	private Price(decimal value, string currency)
	{
		Value = value;
		Currency = currency;
	}

	public static Price Create(decimal value, string currency)
	{
		if (value < 0) throw new DomainException("Price cannot be negative");
		return new Price(value, currency);
	}
}
