using FluentValidation;

namespace Warehouse.Inventory.Application.Inventory.Commands.CreateInventory;

public class CreateInventoryValidator : AbstractValidator<CreateInventoryCommand>
{
	public CreateInventoryValidator()
	{
		RuleFor(x => x.ProductId).NotEmpty()
			.WithMessage("ProductId is required.");

		RuleFor(x => x.Quantity)
			.GreaterThan(0).WithMessage("Quantity must be greater than 0.");
		}
}
