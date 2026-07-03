using Amazon.S3;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Warehouse.ServiceDefaults.BiExport;
using Xunit;

namespace Warehouse.ServiceDefaults.Tests.BiExport;

/// <summary>
/// <see cref="BiExportServiceCollectionExtensions.AddBiExport{TBuilder}"/> is what every service calls
/// in <c>Program.cs</c>; these tests pin the contract that makes it safe to call unconditionally,
/// including with no "BiExport" section at all (local dev, Kubernetes without the bucket wired yet).
/// </summary>
public sealed class BiExportServiceCollectionExtensionsTests
{
    private static HostApplicationBuilder BuilderWithBucket(string? bucketName)
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(
            bucketName is null
                ? []
                : new Dictionary<string, string?> { ["BiExport:BucketName"] = bucketName });
        return builder;
    }

    [Theory]
    [InlineData(null)] // no "BiExport" section configured at all
    [InlineData("")]
    [InlineData("   ")]
    public void An_unconfigured_bucket_registers_the_null_writer_and_no_S3_client(string? bucketName)
    {
        var builder = BuilderWithBucket(bucketName);

        builder.AddBiExport();

        using var provider = builder.Services.BuildServiceProvider();
        Assert.IsType<NullBronzeExportWriter>(provider.GetRequiredService<IBronzeExportWriter>());
        Assert.Null(provider.GetService<IAmazonS3>());
    }

    [Fact]
    public void A_configured_bucket_registers_the_S3_writer_and_an_S3_client()
    {
        var builder = BuilderWithBucket("warehouse-bi-lake");

        builder.AddBiExport();

        using var provider = builder.Services.BuildServiceProvider();
        Assert.IsType<S3ParquetBronzeExportWriter>(provider.GetRequiredService<IBronzeExportWriter>());
        Assert.NotNull(provider.GetService<IAmazonS3>());
    }

    [Fact]
    public void Calling_it_twice_does_not_throw_a_duplicate_registration_error()
    {
        var builder = BuilderWithBucket("warehouse-bi-lake");

        builder.AddBiExport();
        builder.AddBiExport();

        using var provider = builder.Services.BuildServiceProvider();
        Assert.IsType<S3ParquetBronzeExportWriter>(provider.GetRequiredService<IBronzeExportWriter>());
    }
}
