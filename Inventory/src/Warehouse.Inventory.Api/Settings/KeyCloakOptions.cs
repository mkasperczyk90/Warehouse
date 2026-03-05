using System.ComponentModel.DataAnnotations;

namespace Warehouse.Inventory.Api.Settings;

public class KeyCloakOptions
{
	public const string SectionName = "Authentication:Keycloak";

	[Required]
	[Url(ErrorMessage = "Keycloak Authority must be a valid URL.")]
	public string Authority { get; init; } = string.Empty;

	[Required]
	public string ValidIssuer { get; init; } = string.Empty;

	public bool RequireHttpsMetadata { get; init; } = true;

	[Required]
	public string Audience { get; init; } = string.Empty;

	[Required]
	public string MetadataAddress { get; init; } = string.Empty;
}
