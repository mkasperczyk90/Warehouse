using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Testcontainers.LocalStack;
using Xunit;

namespace Warehouse.ServiceDefaults.IntegrationTests;

/// <summary>
/// One LocalStack container (S3-compatible) shared by the whole collection — mirrors the pattern in
/// <c>Warehouse.Warehousing.IntegrationTests.InventoryDatabaseFixture</c> (one Postgres container per
/// collection), just for S3 instead. Verifies <see cref="Warehouse.ServiceDefaults.BiExport"/> actually
/// writes readable objects to something speaking the S3 API, without needing real AWS credentials or
/// a real bucket.
/// </summary>
public sealed class BiExportS3Fixture : IAsyncLifetime
{
    public const string BucketName = "warehouse-bi-lake-test";

    private readonly LocalStackContainer _container = new LocalStackBuilder("localstack/localstack:3.8").Build();

    public IAmazonS3 S3 { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();

        // us-east-1: the one region a bucket-create needs no explicit LocationConstraint for. The
        // production default (BiExportOptions.Region = "eu-central-1") is irrelevant here — the
        // writer only cares that the client is pointed at *some* region, and LocalStack doesn't
        // enforce real region semantics.
        S3 = new AmazonS3Client(
            new BasicAWSCredentials("test", "test"),
            new AmazonS3Config
            {
                ServiceURL = _container.GetConnectionString(),
                ForcePathStyle = true,
                AuthenticationRegion = RegionEndpoint.USEast1.SystemName,
            });

        await S3.PutBucketAsync(BucketName);
    }

    public async ValueTask DisposeAsync()
    {
        S3.Dispose();
        await _container.DisposeAsync();
    }
}

[CollectionDefinition(Name)]
public sealed class BiExportS3CollectionDefinition : ICollectionFixture<BiExportS3Fixture>
{
    public const string Name = "bi-export-s3";
}
