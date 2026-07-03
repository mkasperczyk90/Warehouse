using System.Text;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Warehouse.CdcBridge;
using Xunit;

namespace Warehouse.CdcBridge.Tests;

/// <summary>
/// <see cref="CdcRecordWriter"/> against a mocked <see cref="IAmazonS3"/> — no network, no broker.
/// Debezium's own envelope shape (docs/BI_Plan.md — CDC section, schemas stripped via
/// <c>DEBEZIUM_FORMAT_VALUE_SCHEMAS_ENABLE=false</c>) is passed through byte-for-byte; only the S3
/// key is derived from it.
/// </summary>
public sealed class CdcRecordWriterTests
{
    private const long TsMs = 1751536800123; // 2025-07-03T10:00:00.123Z
    private static readonly DateTimeOffset ExpectedOccurredAt = DateTimeOffset.FromUnixTimeMilliseconds(TsMs);

    private static byte[] DebeziumEnvelope(string schema, string table, long lsn, long tsMs = TsMs, bool includeLsn = true)
    {
        var lsnField = includeLsn ? $"\"lsn\": {lsn}, " : "";
        var json = "{"
            + "\"before\": null, "
            + "\"after\": { \"id\": \"1\", \"sku\": \"MILK\" }, "
            + "\"source\": { "
            + "\"version\": \"2.7.0.Final\", "
            + "\"connector\": \"postgresql\", "
            + $"\"schema\": \"{schema}\", "
            + $"\"table\": \"{table}\", "
            + lsnField
            + "\"db\": \"warehouse\" "
            + "}, "
            + "\"op\": \"u\", "
            + $"\"ts_ms\": {tsMs}"
            + "}";
        return Encoding.UTF8.GetBytes(json);
    }

    private static CdcRecordWriter WriterFor(IAmazonS3 s3, string bucket = "warehouse-bi-lake") =>
        new(s3, Options.Create(new CdcBridgeOptions { BucketName = bucket }), NullLogger<CdcRecordWriter>.Instance);

    [Fact]
    public async Task Writes_to_a_key_derived_from_schema_table_lsn_and_ts_ms()
    {
        var s3 = Substitute.For<IAmazonS3>();
        PutObjectRequest? captured = null;
        s3.PutObjectAsync(Arg.Do<PutObjectRequest>(r => captured = r), Arg.Any<CancellationToken>())
            .Returns(new PutObjectResponse());
        var writer = WriterFor(s3);

        var wrote = await writer.WriteAsync(DebeziumEnvelope("inventory", "stock_items", 123456789));

        Assert.True(wrote);
        Assert.NotNull(captured);
        Assert.Equal("warehouse-bi-lake", captured!.BucketName);
        Assert.Equal(
            $"bronze-cdc/inventory/stock_items/date={ExpectedOccurredAt:yyyy-MM-dd}/123456789.json",
            captured.Key);
    }

    [Fact]
    public async Task Writes_the_original_bytes_unchanged()
    {
        var s3 = Substitute.For<IAmazonS3>();
        var buffer = new MemoryStream();
        s3.PutObjectAsync(Arg.Do<PutObjectRequest>(r => r.InputStream!.CopyTo(buffer)), Arg.Any<CancellationToken>())
            .Returns(new PutObjectResponse());
        var writer = WriterFor(s3);
        var envelope = DebeziumEnvelope("inventory", "stock_items", 1);

        await writer.WriteAsync(envelope);

        Assert.Equal(envelope, buffer.ToArray());
    }

    [Fact]
    public async Task A_redelivery_of_the_same_lsn_overwrites_the_same_key()
    {
        var s3 = Substitute.For<IAmazonS3>();
        var keys = new List<string>();
        s3.PutObjectAsync(Arg.Do<PutObjectRequest>(r => keys.Add(r.Key)), Arg.Any<CancellationToken>())
            .Returns(new PutObjectResponse());
        var writer = WriterFor(s3);
        var envelope = DebeziumEnvelope("inventory", "stock_movements", 999);

        await writer.WriteAsync(envelope);
        await writer.WriteAsync(envelope);

        Assert.Equal(2, keys.Count);
        Assert.Equal(keys[0], keys[1]);
    }

    [Fact]
    public async Task A_message_with_no_source_block_is_skipped_without_calling_S3()
    {
        var s3 = Substitute.For<IAmazonS3>();
        var writer = WriterFor(s3);

        var wrote = await writer.WriteAsync(Encoding.UTF8.GetBytes("""{"not":"a debezium record"}"""));

        Assert.False(wrote);
        await s3.DidNotReceive().PutObjectAsync(Arg.Any<PutObjectRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_missing_lsn_falls_back_to_a_random_id_instead_of_throwing()
    {
        var s3 = Substitute.For<IAmazonS3>();
        PutObjectRequest? captured = null;
        s3.PutObjectAsync(Arg.Do<PutObjectRequest>(r => captured = r), Arg.Any<CancellationToken>())
            .Returns(new PutObjectResponse());
        var writer = WriterFor(s3);

        var wrote = await writer.WriteAsync(DebeziumEnvelope("inventory", "stock_items", 0, includeLsn: false));

        Assert.True(wrote);
        Assert.StartsWith($"bronze-cdc/inventory/stock_items/date={ExpectedOccurredAt:yyyy-MM-dd}/", captured!.Key);
        Assert.EndsWith(".json", captured.Key);
    }
}
