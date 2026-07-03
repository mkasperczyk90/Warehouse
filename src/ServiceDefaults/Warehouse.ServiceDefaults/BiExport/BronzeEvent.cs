namespace Warehouse.ServiceDefaults.BiExport;

/// <summary>
/// One row destined for the Bronze landing zone (docs/BI_Plan.md §5) — the envelope every exported
/// event carries, regardless of which service or which integration event produced it.
/// <para>
/// <see cref="Payload"/> is deliberately a flat <c>string</c> map rather than the raw event object:
/// Bronze is schema-on-read tolerant by design (typing happens in Silver), and keeping the writer
/// ignorant of any specific event type is what makes it reusable across every service without
/// generic constraints or reflection leaking into <see cref="IBronzeExportWriter"/>.
/// </para>
/// </summary>
/// <param name="SourceSystem">Publishing service, e.g. <c>"warehouse-service"</c>.</param>
/// <param name="EventType">The event's own name, e.g. <c>"GoodsReceivedV1"</c>.</param>
/// <param name="EventId">Stable, unique id for this occurrence — becomes part of the S3 key, so a
/// redelivery overwrites the same object instead of duplicating it.</param>
/// <param name="OccurredAt">When the event happened (not when it's being exported) — partitions the
/// landing zone by business date.</param>
/// <param name="Payload">The event's own fields, flattened to strings.</param>
public sealed record BronzeEvent(
    string SourceSystem,
    string EventType,
    string EventId,
    DateTimeOffset OccurredAt,
    IReadOnlyDictionary<string, string?> Payload);
