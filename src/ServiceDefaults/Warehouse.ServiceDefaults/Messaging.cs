using JasperFx.Resources;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.Postgresql;
using Wolverine.RabbitMQ;
using Wolverine.RabbitMQ.Internal;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Shared messaging defaults for the warehouse services. Wolverine owns the transactional outbox:
/// integration events are written to the service's own database in the SAME transaction as the
/// aggregate that produced them, then relayed to RabbitMQ after the commit — so "publish after save"
/// can never lose a message, nor send one for a transaction that rolled back.
///
/// Every service wires the store/transport/outbox identically through <see cref="AddWarehouseMessaging"/>;
/// the per-service <c>configure</c> callback adds only what is service-specific: which messages it
/// publishes to which exchange, which exchanges/queues it listens to, and which assemblies hold its
/// handlers.
/// </summary>
public static class MessagingExtensions
{
    /// <summary>Wolverine's message-store schema (kept out of the domain schemas).</summary>
    public const string MessageStoreSchema = "wolverine";

    /// <summary>
    /// Wires the Postgres message store (on <paramref name="messageStoreConnectionName"/>), EF Core
    /// transactions, the RabbitMQ transport (on the <c>rabbitmq</c> connection, auto-provisioned), and
    /// a durable outbox on all sending endpoints. In Development it also lets Wolverine create its
    /// message-store tables and provision RabbitMQ on startup.
    /// </summary>
    /// <param name="messageStoreConnectionName">Connection-string name of the service's own database
    /// (the outbox lives beside the aggregates, e.g. <c>"logistics"</c>, <c>"warehouse"</c>).</param>
    /// <param name="configure">Service-specific Wolverine setup: which messages it publishes to which
    /// exchange, which exchange→queue bindings it listens to (via the supplied RabbitMQ expression),
    /// and which assemblies hold its handlers.</param>
    public static TBuilder AddWarehouseMessaging<TBuilder>(
        this TBuilder builder,
        string messageStoreConnectionName,
        Action<WolverineOptions, RabbitMqTransportExpression> configure)
        where TBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull(configure);

        builder.UseWolverine(opts =>
        {
            // Handler dependencies (the module repositories/ledgers/outbox) are deliberately `internal`
            // to the Infrastructure layer. Wolverine 6 flipped the codegen default to
            // ServiceLocationPolicy.NotAllowed, which then refuses to generate any handler whose concrete
            // dependency is non-public — so EVERY cross-service event consumer (goods-receipt, picks,
            // reservations, replica updaters) silently failed to build and never ran. Restore the 5.x
            // behaviour: resolve those services from the container (service location) instead of inline.
            opts.ServiceLocationPolicy = JasperFx.CodeGeneration.Model.ServiceLocationPolicy.AllowedButWarn;

            opts.PersistMessagesWithPostgresql(
                builder.Configuration.GetConnectionString(messageStoreConnectionName)!, schemaName: MessageStoreSchema);
            opts.UseEntityFrameworkCoreTransactions();

            var rabbit = opts.UseRabbitMq(new Uri(builder.Configuration.GetConnectionString("rabbitmq")!)).AutoProvision();

            // Durable: the message lands in the outbox table first, so the relay survives a crash.
            opts.Policies.UseDurableOutboxOnAllSendingEndpoints();

            configure(opts, rabbit);
        });

        // Surface the broker in readiness: Wolverine owns its own RabbitMQ connection and does not register
        // a health check, so without this a service reports Healthy while unable to relay the outbox. The
        // Aspire client adds a "rabbitmq" connectivity check to /health (untagged → not part of /alive).
        builder.AddRabbitMQClient("rabbitmq");

        // Dev: let Wolverine create its message-store tables and provision the RabbitMQ topology on startup.
        if (builder.Environment.IsDevelopment())
        {
            builder.Services.AddResourceSetupOnStartup();
        }

        return builder;
    }
}
