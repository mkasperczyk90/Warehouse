using Amazon;
using Amazon.S3;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Warehouse.ServiceDefaults.BiExport;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Wires BI export (docs/BI_Plan.md §11) the same way every other cross-cutting concern in this
/// project is wired: one call in the host's <c>Program.cs</c>, opt-in per service. Reads the
/// <c>"BiExport"</c> section; an empty/missing <c>BucketName</c> registers
/// <see cref="NullBronzeExportWriter"/> instead of the S3 client, so a service that hasn't configured
/// export still builds and runs exactly as before — nothing downstream has to branch on whether
/// export is on.
/// </summary>
public static class BiExportServiceCollectionExtensions
{
    public static TBuilder AddBiExport<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        var options = builder.Configuration.GetSection(BiExportOptions.SectionName).Get<BiExportOptions>()
            ?? new BiExportOptions();

        builder.Services.Configure<BiExportOptions>(builder.Configuration.GetSection(BiExportOptions.SectionName));

        if (options.IsEnabled)
        {
            // Credentials come from the AWS SDK's default credential chain (the ECS task role in the
            // deployed environment, see infra/terraform/environment/ecs.tf — never static keys), so
            // the client only needs a region, not full AWSOptions/AWSSDK.Extensions.NETCore.Setup.
            builder.Services.AddSingleton<IAmazonS3>(
                _ => new AmazonS3Client(RegionEndpoint.GetBySystemName(options.Region)));
            builder.Services.AddSingleton<IBronzeExportWriter, S3ParquetBronzeExportWriter>();
        }
        else
        {
            builder.Services.AddSingleton<IBronzeExportWriter, NullBronzeExportWriter>();
        }

        return builder;
    }
}
