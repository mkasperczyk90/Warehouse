namespace Warehouse.Inventory.Infrastructure.Settings;

public class ProductApiOptions
{
	public const string SectionName = "Services:ProductApi";

	public string BaseUrl { get; init; } = string.Empty;
	public int TimeoutSeconds { get; init; } = 5;
	public int MaxRetryAttempts { get; init; } = 3;
}
