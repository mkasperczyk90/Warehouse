using FluentValidation;

namespace Warehouse.Product.Application.Products.Commands.CreateProduct;

public class CreateProductValidator : AbstractValidator<CreateProductCommand>
{
	public CreateProductValidator()
	{
		RuleFor(x => x.Name)
			.NotEmpty().WithMessage("Name is required.")
			.MaximumLength(200).WithMessage("Name cannot exceed 200 characters.");

		RuleFor(x => x.Price)
			.GreaterThan(0).WithMessage("Price must be greater than 0.");
		}
}
