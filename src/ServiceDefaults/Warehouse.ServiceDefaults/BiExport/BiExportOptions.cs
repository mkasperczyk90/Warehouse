namespace Warehouse.ServiceDefaults.BiExport;

/// <summary>
/// Bound from the <c>"BiExport"</c> configuration section (docs/BI_Plan.md §11). An empty
/// <see cref="BucketName"/> — the default — disables export: <see cref="AddBiExport"/> then wires up
/// <see cref="NullBronzeExportWriter"/> instead of the S3 writer, so callers never need to check
/// "is this enabled" themselves.
/// </summary>
public sealed class BiExportOptions
{
    public const string SectionName = "BiExport";

    public string BucketName { get; init; } = "";

    public string Region { get; init; } = "eu-central-1";

    public bool IsEnabled => !string.IsNullOrWhiteSpace(BucketName);
}
