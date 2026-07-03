using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Warehouse.CdcBridge;

/// <summary>
/// Lands one raw Debezium change-event JSON document per S3 object, at
/// <c>bronze-cdc/&lt;schema&gt;/&lt;table&gt;/date=yyyy-MM-dd/&lt;lsn&gt;.json</c> — sibling to the
/// event-driven writer's <c>bronze/</c> prefix (docs/BI_Plan.md — CDC section). The document is
/// written byte-for-byte as Debezium produced it (its native <c>before</c>/<c>after</c>/<c>op</c>/
/// <c>source</c> envelope, schemas stripped via <c>DEBEZIUM_FORMAT_VALUE_SCHEMAS_ENABLE=false</c>),
/// so it plugs directly into Databricks' <c>APPLY CHANGES INTO</c> without a custom transform.
/// <para>
/// The key is deterministic on the Postgres LSN (<c>source.lsn</c>) rather than a random id: the LSN
/// is the physical WAL position of the change, so a redelivery of the same change always produces
/// the same key and overwrites the same object instead of duplicating a row downstream — the same
/// idempotency property <c>S3ParquetBronzeExportWriter</c> gets from the Wolverine envelope id.
/// </para>
/// </summary>
public sealed partial class CdcRecordWriter(
    IAmazonS3 s3, IOptions<CdcBridgeOptions> options, ILogger<CdcRecordWriter> logger)
{
    private readonly CdcBridgeOptions _options = options.Value;

    /// <returns><c>true</c> if the message was a recognizable Debezium change event and was written;
    /// <c>false</c> if it had no <c>source</c> block and was skipped (e.g. a heartbeat/tombstone).</returns>
    public async Task<bool> WriteAsync(ReadOnlyMemory<byte> body, CancellationToken cancellationToken = default)
    {
        if (!TryParseEnvelope(body, out var record))
        {
            LogSkippedUnparseable(logger);
            return false;
        }

        var key = BuildKey(record);

        using var stream = new MemoryStream(body.ToArray());
        await s3.PutObjectAsync(
            new PutObjectRequest
            {
                BucketName = _options.BucketName,
                Key = key,
                InputStream = stream,
                AutoCloseStream = false,
                ContentType = "application/json",
            },
            cancellationToken);

        LogWrote(logger, record.Schema, record.Table, _options.BucketName, key);
        return true;
    }

    private static string BuildKey(CdcEnvelope record) =>
        $"bronze-cdc/{record.Schema}/{record.Table}/date={record.OccurredAt:yyyy-MM-dd}/{record.Lsn}.json";

    private static bool TryParseEnvelope(ReadOnlyMemory<byte> body, out CdcEnvelope record)
    {
        record = default;

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        if (!root.TryGetProperty("source", out var source) ||
            !source.TryGetProperty("schema", out var schemaProp) ||
            !source.TryGetProperty("table", out var tableProp))
        {
            return false;
        }

        // Raw JSON token text works whether lsn/ts_ms arrive as a JSON number or (defensively) a
        // string — avoids committing to GetInt64() against a field shape only Debezium controls.
        var lsn = source.TryGetProperty("lsn", out var lsnProp) ? lsnProp.GetRawText() : Guid.NewGuid().ToString();
        var occurredAt = root.TryGetProperty("ts_ms", out var tsProp) && tsProp.TryGetInt64(out var tsMs)
            ? DateTimeOffset.FromUnixTimeMilliseconds(tsMs)
            : DateTimeOffset.UtcNow;

        record = new CdcEnvelope(schemaProp.GetString() ?? "unknown", tableProp.GetString() ?? "unknown", lsn, occurredAt);
        return true;
    }

    private readonly record struct CdcEnvelope(string Schema, string Table, string Lsn, DateTimeOffset OccurredAt);

    [LoggerMessage(Level = LogLevel.Debug, Message = "CDC bridge: wrote {Schema}.{Table} to s3://{Bucket}/{Key}")]
    private static partial void LogWrote(ILogger logger, string schema, string table, string bucket, string key);

    [LoggerMessage(Level = LogLevel.Warning, Message = "CDC bridge: skipped a message with no Debezium 'source' block")]
    private static partial void LogSkippedUnparseable(ILogger logger);
}
