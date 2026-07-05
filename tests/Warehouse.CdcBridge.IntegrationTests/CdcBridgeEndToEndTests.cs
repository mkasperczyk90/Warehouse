using System.Text;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Testcontainers.LocalStack;
using Testcontainers.RabbitMq;
using Warehouse.CdcBridge;
using Xunit;

namespace Warehouse.CdcBridge.IntegrationTests;

/// <summary>
/// End-to-end proof that <see cref="CdcListenerService"/> actually consumes off a real RabbitMQ
/// broker (Testcontainers) and lands the message in a real S3-compatible endpoint (LocalStack) —
/// the unit tests in <c>Warehouse.CdcBridge.Tests</c> mock both entirely, so this is the only place
/// that exercises the real AMQP wire protocol (exchange/queue declare, bind, consume, ack) this
/// project's Debezium-facing design depends on (docs/BI_Plan.md — CDC section).
/// </summary>
public sealed class CdcBridgeEndToEndTests : IAsyncLifetime
{
    private const string Exchange = "cdc.test";
    private const string Bucket = "warehouse-bi-lake-test";

    private readonly RabbitMqContainer _rabbitMq = new RabbitMqBuilder("rabbitmq:3-management-alpine").Build();
    private readonly LocalStackContainer _localStack = new LocalStackBuilder("localstack/localstack:3.8").Build();

    private AmazonS3Client _s3 = null!;
    private IConnection _publisherConnection = null!;
    private IChannel _publisherChannel = null!;
    private CdcListenerService _listener = null!;

    public async ValueTask InitializeAsync()
    {
        await Task.WhenAll(_rabbitMq.StartAsync(), _localStack.StartAsync());

        _s3 = new AmazonS3Client(
            new BasicAWSCredentials("test", "test"),
            new AmazonS3Config
            {
                ServiceURL = _localStack.GetConnectionString(),
                ForcePathStyle = true,
                AuthenticationRegion = RegionEndpoint.USEast1.SystemName,
            });
        await _s3.PutBucketAsync(Bucket);

        var options = Options.Create(new CdcBridgeOptions
        {
            BucketName = Bucket,
            RabbitMqHost = _rabbitMq.Hostname,
            RabbitMqPort = _rabbitMq.GetMappedPublicPort(5672),
            RabbitMqUsername = RabbitMqBuilder.DefaultUsername,
            RabbitMqPassword = RabbitMqBuilder.DefaultPassword,
            Exchanges = [Exchange],
        });
        var writer = new CdcRecordWriter(_s3, options, NullLogger<CdcRecordWriter>.Instance);
        _listener = new CdcListenerService(options, writer, NullLogger<CdcListenerService>.Instance);
        await _listener.StartAsync(CancellationToken.None);

        // The publisher side stands in for a Debezium Server instance: declares the exchange (which
        // rabbitmq-init would declare in the real deployment, see infra/terraform/environment/locals.tf)
        // and publishes a raw change-event JSON with a routing key, exactly like the RabbitMQ sink does.
        var factory = new ConnectionFactory
        {
            HostName = _rabbitMq.Hostname,
            Port = _rabbitMq.GetMappedPublicPort(5672),
            UserName = RabbitMqBuilder.DefaultUsername,
            Password = RabbitMqBuilder.DefaultPassword,
        };
        _publisherConnection = await factory.CreateConnectionAsync();
        _publisherChannel = await _publisherConnection.CreateChannelAsync();
        await _publisherChannel.ExchangeDeclareAsync(Exchange, ExchangeType.Topic, true);

        // Give the listener's queue declare/bind/consume a moment to land before we publish —
        // avoids a flaky race where the message arrives before the queue exists.
        await Task.Delay(TimeSpan.FromSeconds(2));
    }

    public async ValueTask DisposeAsync()
    {
        await _listener.StopAsync(CancellationToken.None);
        _listener.Dispose();
        await _publisherChannel.CloseAsync();
        await _publisherConnection.CloseAsync();
        _s3.Dispose();
        await _rabbitMq.DisposeAsync();
        await _localStack.DisposeAsync();
    }

    [Fact]
    public async Task A_published_Debezium_record_lands_in_S3_via_the_real_broker()
    {
        const string json = """
            {
              "before": null,
              "after": { "id": "1", "sku": "MILK" },
              "source": { "schema": "inventory", "table": "stock_items", "lsn": 42 },
              "op": "c",
              "ts_ms": 1751536800123
            }
            """;

        await _publisherChannel.BasicPublishAsync(Exchange, "warehouse.inventory.stock_items", Encoding.UTF8.GetBytes(json));

        var expectedKey = $"bronze-cdc/inventory/stock_items/date={DateTimeOffset.FromUnixTimeMilliseconds(1751536800123):yyyy-MM-dd}/42.json";
        GetObjectResponse? response = null;
        for (var attempt = 0; attempt < 20 && response is null; attempt++)
        {
            try
            {
                response = await _s3.GetObjectAsync(Bucket, expectedKey);
            }
            catch (AmazonS3Exception)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(500));
            }
        }

        Assert.NotNull(response);
        using (response)
        {
            using var reader = new StreamReader(response!.ResponseStream);
            var body = await reader.ReadToEndAsync();
            Assert.Contains("\"sku\": \"MILK\"", body);
        }
    }
}
