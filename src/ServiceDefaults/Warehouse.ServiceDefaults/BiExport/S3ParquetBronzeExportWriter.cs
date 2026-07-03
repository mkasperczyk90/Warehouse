using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Parquet;
using Parquet.Schema;

namespace Warehouse.ServiceDefaults.BiExport;

/// <summary>
/// Writes one Parquet file per event to S3, at
/// <c>bronze/&lt;source_system&gt;/&lt;event_type&gt;/date=yyyy-MM-dd/&lt;event_id&gt;.parquet</c>
/// (docs/BI_Plan.md §5/§11) — Databricks Auto Loader watches that prefix. The key is deterministic
/// on <see cref="BronzeEvent.EventId"/>, so a redelivery from the at-least-once outbox overwrites the
/// same object instead of duplicating a row downstream.
/// </summary>
public sealed partial class S3ParquetBronzeExportWriter(
    IAmazonS3 s3,
    IOptions<BiExportOptions> options,
    ILogger<S3ParquetBronzeExportWriter> logger) : IBronzeExportWriter
{
    private readonly BiExportOptions _options = options.Value;

    public async Task WriteAsync(BronzeEvent bronzeEvent, CancellationToken cancellationToken = default)
    {
        var key = BuildKey(bronzeEvent);

        using var stream = new MemoryStream();
        await WriteParquetAsync(stream, bronzeEvent, cancellationToken);
        stream.Position = 0;

        await s3.PutObjectAsync(
            new PutObjectRequest
            {
                BucketName = _options.BucketName,
                Key = key,
                InputStream = stream,
                AutoCloseStream = false,
            },
            cancellationToken);

        LogWrote(logger, bronzeEvent.EventType, bronzeEvent.EventId, _options.BucketName, key);
    }

    private static string BuildKey(BronzeEvent bronzeEvent) =>
        $"bronze/{bronzeEvent.SourceSystem}/{bronzeEvent.EventType}/date={bronzeEvent.OccurredAt:yyyy-MM-dd}/{bronzeEvent.EventId}.parquet";

    // One row per file: the simplest possible event->Parquet mapping, and it keeps every file
    // self-contained and idempotent by key alone. Nothing here requires 1 file = 1 row — if export
    // volume ever justifies it, batch several events into one file before writing (e.g. flush on a
    // timer/size threshold) without changing IBronzeExportWriter's contract.
    private static async Task WriteParquetAsync(Stream stream, BronzeEvent bronzeEvent, CancellationToken cancellationToken)
    {
        var columnNames = new List<string> { "_source_system", "_event_type", "_event_id", "_occurred_at" };
        var columnValues = new List<string?>
        {
            bronzeEvent.SourceSystem,
            bronzeEvent.EventType,
            bronzeEvent.EventId,
            bronzeEvent.OccurredAt.ToString("O"),
        };

        // Bronze is schema-on-read tolerant (docs/BI_Plan.md §5): every payload field lands as a
        // string here; Silver is where real typing happens.
        foreach (var (name, value) in bronzeEvent.Payload)
        {
            columnNames.Add(name);
            columnValues.Add(value);
        }

        var fields = columnNames.Select(name => new DataField<string>(name)).ToArray();
        var schema = new ParquetSchema(fields);

        await using var writer = await ParquetWriter.CreateAsync(schema, stream, cancellationToken: cancellationToken);
        using var rowGroup = writer.CreateRowGroup();
        for (var i = 0; i < fields.Length; i++)
        {
            // Every column carries exactly one value (§ one row per file, above); the string overload
            // handles nulls in the values collection even though the field itself is declared non-nullable.
            await rowGroup.WriteAsync(fields[i], new[] { columnValues[i] });
        }
    }

    // Source-generated (compile-time) logging: no boxing/array allocation on the hot path when
    // Debug is disabled, unlike calling ILogger.LogDebug(...) directly (CA1873).
    [LoggerMessage(Level = LogLevel.Debug, Message = "BI export: wrote {EventType}/{EventId} to s3://{Bucket}/{Key}")]
    private static partial void LogWrote(ILogger logger, string eventType, string eventId, string bucket, string key);
}
