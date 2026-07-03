namespace Warehouse.ServiceDefaults.BiExport;

/// <summary>
/// Lands a <see cref="BronzeEvent"/> in the Bronze zone (docs/BI_Plan.md §5/§11). Consumers (one per
/// integration event a service wants exported — see
/// <c>Warehouse.Warehousing.Inventory.Application.BiExport</c> for the reference implementation)
/// depend only on this interface, never on <see cref="S3ParquetBronzeExportWriter"/> directly, so a
/// unit test can substitute a fake and the transport can change without touching a single consumer.
/// </summary>
public interface IBronzeExportWriter
{
    Task WriteAsync(BronzeEvent bronzeEvent, CancellationToken cancellationToken = default);
}
