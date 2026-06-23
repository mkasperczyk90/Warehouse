using NSubstitute;
using Warehouse.Logistics.Core.Infrastructure.Persistence;
using Wolverine.EntityFrameworkCore;

namespace Warehouse.Logistics.Tests.TestDoubles;

/// <summary>
/// A substitute transactional outbox for handler tests. NSubstitute auto-returns completed tasks for
/// the async members, so the handlers' <c>PublishAsync</c> + <c>SaveChangesAndFlushMessagesAsync</c>
/// calls just work; we then read back what was published.
/// </summary>
internal static class Outbox
{
    public static IDbContextOutbox<LogisticsDbContext> Create() =>
        Substitute.For<IDbContextOutbox<LogisticsDbContext>>();

    /// <summary>Every message handed to <c>PublishAsync</c>, in call order. Signature-agnostic so it
    /// survives whether Wolverine's overload is generic or takes <c>object</c>.</summary>
    public static IReadOnlyList<object> Published(this IDbContextOutbox<LogisticsDbContext> outbox) =>
        outbox.ReceivedCalls()
            .Where(c => c.GetMethodInfo().Name == "PublishAsync")
            .Select(c => c.GetArguments()[0]!)
            .ToList();

    /// <summary>The single published message of type <typeparamref name="T"/> (fails the assertion if
    /// there isn't exactly one).</summary>
    public static T PublishedMessage<T>(this IDbContextOutbox<LogisticsDbContext> outbox) =>
        outbox.Published().OfType<T>().Single();
}
