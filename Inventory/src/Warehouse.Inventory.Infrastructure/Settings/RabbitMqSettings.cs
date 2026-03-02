using System.ComponentModel.DataAnnotations;

namespace Warehouse.Inventory.Infrastructure.Settings;

public class RabbitMqSettings
{
	public const string SectionName = "RabbitMq";

	[Required]
	public string HostName { get; init; } = string.Empty;

	[Required]
	public int Port { get; init; }

	[Required]
	public string UserName { get; init; } = string.Empty;

	[Required]
	public string Password { get; init; } = string.Empty;
}
