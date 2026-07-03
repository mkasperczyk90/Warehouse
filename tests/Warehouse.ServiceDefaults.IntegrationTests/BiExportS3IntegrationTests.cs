using Amazon.S3.Model;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Parquet;
using Parquet.Schema;
using Warehouse.ServiceDefaults.BiExport;
using Xunit;

namespace Warehouse.ServiceDefaults.IntegrationTests;

/// <summary>
/// End-to-end proof that <see cref="S3ParquetBronzeExportWriter"/> lands a real, readable Parquet
/// object at an S3-compatible endpoint (docs/BI_Plan.md §11) — the unit tests in
/// <c>Warehouse.ServiceDefaults.Tests</c> mock <c>IAmazonS3</c> entirely, so this is the only place
/// that exercises the actual AWS SDK request/response wire format against a server.
/// </summary>
[Collection(BiExportS3CollectionDefinition.Name)]
public sealed class BiExportS3IntegrationTests(BiExportS3Fixture fixture)
{
    private static BronzeEvent SampleEvent(string eventId) => new(
        SourceSystem: "warehouse-service",
        EventType: "GoodsReceivedV1",
        EventId: eventId,
        OccurredAt: new DateTimeOffset(2026, 7, 3, 10, 0, 0, TimeSpan.Zero),
        Payload: new Dictionary<string, string?>
        {
            ["delivery_id"] = "DEL-1",
            ["warehouse_code"] = "WH-01",
        });

    private S3ParquetBronzeExportWriter Writer =>
        new(fixture.S3, Options.Create(new BiExportOptions { BucketName = BiExportS3Fixture.BucketName }),
            NullLogger<S3ParquetBronzeExportWriter>.Instance);

    [Fact]
    public async Task Lands_a_readable_Parquet_object_at_the_expected_bronze_key()
    {
        var bronzeEvent = SampleEvent(Guid.NewGuid().ToString());

        await Writer.WriteAsync(bronzeEvent);

        var expectedKey = $"bronze/warehouse-service/GoodsReceivedV1/date=2026-07-03/{bronzeEvent.EventId}.parquet";
        using var response = await fixture.S3.GetObjectAsync(BiExportS3Fixture.BucketName, expectedKey);
        using var buffer = new MemoryStream();
        await response.ResponseStream.CopyToAsync(buffer);
        buffer.Position = 0;

        await using var reader = await ParquetReader.CreateAsync(buffer);
        using var rowGroup = reader.OpenRowGroupReader(0);
        Assert.Equal(1L, rowGroup.RowCount);

        Assert.Equal("DEL-1", await ReadSingleAsync(reader, rowGroup, "delivery_id"));
    }

    [Fact]
    public async Task Redelivering_the_same_event_overwrites_the_object_instead_of_creating_a_duplicate()
    {
        var eventId = Guid.NewGuid().ToString();

        await Writer.WriteAsync(SampleEvent(eventId) with
        {
            Payload = new Dictionary<string, string?> { ["delivery_id"] = "FIRST" },
        });
        await Writer.WriteAsync(SampleEvent(eventId) with
        {
            Payload = new Dictionary<string, string?> { ["delivery_id"] = "SECOND" },
        });

        var key = $"bronze/warehouse-service/GoodsReceivedV1/date=2026-07-03/{eventId}.parquet";
        using var response = await fixture.S3.GetObjectAsync(BiExportS3Fixture.BucketName, key);
        using var buffer = new MemoryStream();
        await response.ResponseStream.CopyToAsync(buffer);
        buffer.Position = 0;

        await using var reader = await ParquetReader.CreateAsync(buffer);
        using var rowGroup = reader.OpenRowGroupReader(0);
        Assert.Equal("SECOND", await ReadSingleAsync(reader, rowGroup, "delivery_id"));
    }

    // Fields must come from the file's own schema (not a locally re-constructed DataField<string>) so
    // MaxDefinitionLevel matches what was actually written — see the same helper in
    // Warehouse.ServiceDefaults.Tests.BiExport.S3ParquetBronzeExportWriterTests.
    private static async Task<string?> ReadSingleAsync(ParquetReader reader, ParquetRowGroupReader rowGroup, string column)
    {
        var field = reader.Schema.DataFields.Single(f => f.Name == column);
        var values = new string?[1];
        await rowGroup.ReadAsync(field, values);
        return values[0];
    }
}
