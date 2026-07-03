using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Parquet;
using Parquet.Schema;
using Warehouse.ServiceDefaults.BiExport;
using Xunit;

namespace Warehouse.ServiceDefaults.Tests.BiExport;

/// <summary>
/// Exercises <see cref="S3ParquetBronzeExportWriter"/> against a mocked <see cref="IAmazonS3"/> — no
/// network, no container. The uploaded bytes are captured and read back with <see cref="ParquetReader"/>
/// to prove the file is actually valid Parquet, not just that some bytes were sent somewhere.
/// </summary>
public sealed class S3ParquetBronzeExportWriterTests
{
    private static BronzeEvent SampleEvent() => new(
        SourceSystem: "warehouse-service",
        EventType: "GoodsReceivedV1",
        EventId: "11111111-1111-1111-1111-111111111111",
        OccurredAt: new DateTimeOffset(2026, 7, 3, 10, 0, 0, TimeSpan.Zero),
        Payload: new Dictionary<string, string?>
        {
            ["delivery_id"] = "DEL-1",
            ["warehouse_code"] = "WH-01",
            ["line_count"] = "3",
        });

    private static S3ParquetBronzeExportWriter WriterFor(IAmazonS3 s3, string bucket = "warehouse-bi-lake") =>
        new(s3, Options.Create(new BiExportOptions { BucketName = bucket }), NullLogger<S3ParquetBronzeExportWriter>.Instance);

    [Fact]
    public async Task Writes_to_the_deterministic_bronze_key_derived_from_the_event()
    {
        var s3 = Substitute.For<IAmazonS3>();
        PutObjectRequest? captured = null;
        s3.PutObjectAsync(Arg.Do<PutObjectRequest>(r => captured = r), Arg.Any<CancellationToken>())
            .Returns(new PutObjectResponse());
        var writer = WriterFor(s3);

        await writer.WriteAsync(SampleEvent());

        Assert.NotNull(captured);
        Assert.Equal("warehouse-bi-lake", captured!.BucketName);
        Assert.Equal(
            "bronze/warehouse-service/GoodsReceivedV1/date=2026-07-03/11111111-1111-1111-1111-111111111111.parquet",
            captured.Key);
    }

    [Fact]
    public async Task A_redelivery_of_the_same_event_overwrites_the_same_key_instead_of_a_near_duplicate()
    {
        var s3 = Substitute.For<IAmazonS3>();
        var keys = new List<string>();
        s3.PutObjectAsync(Arg.Do<PutObjectRequest>(r => keys.Add(r.Key)), Arg.Any<CancellationToken>())
            .Returns(new PutObjectResponse());
        var writer = WriterFor(s3);

        await writer.WriteAsync(SampleEvent());
        await writer.WriteAsync(SampleEvent());

        Assert.Equal(2, keys.Count);
        Assert.Equal(keys[0], keys[1]);
    }

    [Fact]
    public async Task The_uploaded_stream_is_a_valid_Parquet_file_carrying_the_envelope_and_payload_columns()
    {
        var s3 = Substitute.For<IAmazonS3>();
        var buffer = new MemoryStream();
        s3.PutObjectAsync(Arg.Do<PutObjectRequest>(r => r.InputStream!.CopyTo(buffer)), Arg.Any<CancellationToken>())
            .Returns(new PutObjectResponse());
        var writer = WriterFor(s3);

        await writer.WriteAsync(SampleEvent());

        buffer.Position = 0;
        await using var reader = await ParquetReader.CreateAsync(buffer);
        Assert.Equal(1, reader.RowGroupCount);
        using var rowGroup = reader.OpenRowGroupReader(0);
        Assert.Equal(1L, rowGroup.RowCount);

        // Fields must come from the file's own schema (not a locally re-constructed DataField<string>)
        // so MaxDefinitionLevel matches what was actually written.
        var fields = reader.Schema.DataFields;

        Assert.Equal("warehouse-service", await ReadSingleAsync(rowGroup, fields, "_source_system"));
        Assert.Equal("GoodsReceivedV1", await ReadSingleAsync(rowGroup, fields, "_event_type"));
        Assert.Equal("11111111-1111-1111-1111-111111111111", await ReadSingleAsync(rowGroup, fields, "_event_id"));
        Assert.Equal("2026-07-03T10:00:00.0000000+00:00", await ReadSingleAsync(rowGroup, fields, "_occurred_at"));
        Assert.Equal("DEL-1", await ReadSingleAsync(rowGroup, fields, "delivery_id"));
        Assert.Equal("WH-01", await ReadSingleAsync(rowGroup, fields, "warehouse_code"));
        Assert.Equal("3", await ReadSingleAsync(rowGroup, fields, "line_count"));
    }

    private static async Task<string?> ReadSingleAsync(ParquetRowGroupReader rowGroup, DataField[] fields, string column)
    {
        var field = fields.Single(f => f.Name == column);
        var values = new string?[1];
        await rowGroup.ReadAsync(field, values);
        return values[0];
    }
}
