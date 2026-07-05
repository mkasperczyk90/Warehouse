using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Warehouse.CdcBridge;

/// <summary>
/// Consumes Debezium's raw CDC JSON straight off RabbitMQ (plain AMQP — Debezium's
/// <c>debezium-server-rabbitmq</c> sink publishes with the standard <c>com.rabbitmq.client</c>, not a
/// Kafka-shaped protocol) and hands each message to <see cref="CdcRecordWriter"/>. One durable queue
/// per source-database exchange (docs/BI_Plan.md — CDC section), bound with routing key <c>#</c>:
/// table-level filtering already happened in each Debezium connector's <c>table.include.list</c>, so
/// this bridge doesn't need to re-filter by routing key.
/// <para>
/// Deliberately a raw <see cref="RabbitMQ.Client"/> consumer, not a Wolverine listener — these
/// messages aren't typed integration events with a C# contract, they're Debezium's own JSON envelope,
/// so forcing them through Wolverine's message-type routing would fight the grain rather than help.
/// </para>
/// </summary>
public sealed class CdcListenerService(
    IOptions<CdcBridgeOptions> options, CdcRecordWriter writer, ILogger<CdcListenerService> logger)
    : BackgroundService
{
    private readonly CdcBridgeOptions _options = options.Value;
    private IConnection? _connection;
    private IChannel? _channel;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _options.RabbitMqHost,
            Port = _options.RabbitMqPort,
            UserName = _options.RabbitMqUsername,
            Password = _options.RabbitMqPassword,
        };

        _connection = await factory.CreateConnectionAsync(stoppingToken);
        _channel = await _connection.CreateChannelAsync(null, stoppingToken);

        foreach (var exchange in _options.Exchanges)
        {
            await BindAndConsumeAsync(_channel, exchange, stoppingToken);
        }

        // The consumer callbacks below do the actual work; keep the host alive until shutdown.
        await Task.Delay(Timeout.Infinite, stoppingToken).ContinueWith(
            _ => { }, TaskScheduler.Default);
    }

    private async Task BindAndConsumeAsync(IChannel channel, string exchange, CancellationToken stoppingToken)
    {
        // Declares the exchange itself rather than assuming rabbitmq-init already ran (idempotent —
        // redeclaring an existing exchange with identical type/durability is a no-op in RabbitMQ).
        // Removes a hard startup-ordering dependency: this bridge can now come up standalone (e.g. in
        // local Aspire orchestration, where there's no rabbitmq-init step) without crashing on a
        // bind-to-nonexistent-exchange error; in ECS it still matches what rabbitmq-init declares.
        await channel.ExchangeDeclareAsync(exchange, "topic", durable: true, autoDelete: false, arguments: null, false, false, stoppingToken);

        var queueName = $"cdc-bridge.{exchange}";
        await channel.QueueDeclareAsync(queueName, true, false, false, null, false, false, stoppingToken);
        await channel.QueueBindAsync(queueName, exchange, "#", null, false, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, delivery) => await HandleDeliveryAsync(channel, exchange, delivery, stoppingToken);

        await channel.BasicConsumeAsync(queueName, false, string.Empty, false, false, null, consumer, stoppingToken);
    }

    private async Task HandleDeliveryAsync(
        IChannel channel, string exchange, BasicDeliverEventArgs delivery, CancellationToken stoppingToken)
    {
        try
        {
            await writer.WriteAsync(delivery.Body, stoppingToken);
            await channel.BasicAckAsync(delivery.DeliveryTag, false, stoppingToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Requeue: a transient S3/network failure shouldn't drop a CDC record. A message that
            // fails deterministically (e.g. malformed JSON) will keep being requeued and logged —
            // acceptable for now; a dead-letter exchange is a hardening item, not a day-1 requirement
            // for this ephemeral environment.
            logger.LogError(ex, "CDC bridge: failed to write a message from exchange '{Exchange}'; requeued", exchange);
            await channel.BasicNackAsync(delivery.DeliveryTag, false, true, stoppingToken);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel is not null)
        {
            await _channel.CloseAsync(cancellationToken);
        }

        if (_connection is not null)
        {
            await _connection.CloseAsync(cancellationToken);
        }

        await base.StopAsync(cancellationToken);
    }
}
