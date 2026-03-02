using System.ComponentModel.DataAnnotations;

namespace Warehouse.Product.Api.Settings;

public class ApplicationSettings
{
	public const string SectionName = "Application";

	[Required]
	public required string Name { get; init; }
}
