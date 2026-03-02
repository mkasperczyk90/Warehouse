using Warehouse.Product.Domain.Products.Exceptions;
using Warehouse.SharedKernel;
using Warehouse.SharedKernel.Exceptions;

namespace Warehouse.Product.Domain.Products.Entities;

public class Product: Entity
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
	private Product() { }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

	private Product(string name, decimal price, string description)
	{
		if (string.IsNullOrWhiteSpace(name)) throw new ProductNameRequireException();
		if (price < 0) throw new NegativeProductPriceException(price);

		Id = Guid.NewGuid();
		Name = name;
		Price = price;
		Amount = 0;
		CreatedAt = DateTime.UtcNow; // TODO: Use TimeProvider
		Description = description;
	}

	public Guid Id { get; private set; }
	public string Name { get; private set; }
	public string Description { get; private set; }
	public decimal Price { get; private set; }
	public int Amount { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime? UpdatedAt { get; private set; }

	public void IncreaseStock(int quantity)
	{
		if (quantity <= 0)
			throw new DomainException("Quantity to add must be positive.");

		Amount += quantity;
		UpdatedAt = DateTime.UtcNow; // TODO: Use TimeProvider
	}

	public void DecreaseStock(int quantity)
	{
		if (Amount - quantity <= 0)
			throw new DomainException("Amount is to small - quantity should be bigger.");

		Amount -= quantity;
		UpdatedAt = DateTime.UtcNow;
	}

	public static Product Create(string name, decimal price, string description) => new(name, price, description);
}
