namespace Warehouse.ServiceDefaults.BiExport;

/// <summary>Registered when BI export is disabled (no bucket configured) — a discard, not a no-op you
/// have to remember to guard against at every call site.</summary>
internal sealed class NullBronzeExportWriter : IBronzeExportWriter
{
    public Task WriteAsync(BronzeEvent bronzeEvent, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
