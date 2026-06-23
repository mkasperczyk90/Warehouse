using NSubstitute;
using Warehouse.Warehousing.Inventory.Infrastructure.Persistence;
using Wolverine.EntityFrameworkCore;

namespace Warehouse.Warehousing.Tests.TestDoubles;

/// <summary>
/// A substitute transactional outbox for consumer tests. NSubstitute auto-returns completed tasks for
/// the async members, so the consumers' <c>PublishAsync</c> + <c>SaveChangesAndFlushMessagesAsync</c>
/// calls just work; we then read back what was published.
/// </summary>
internal static class Outbox
{
    public static IDbContextOutbox<InventoryDbContext> Create() =>
        Substitute.For<IDbContextOutbox<InventoryDbContext>>();

    /// <summary>Every message handed to <c>PublishAsync</c>, in call order. Signature-agnostic so it
    /// survives whether Wolverine's overload is generic or takes <c>object</c>.</summary>
    public static IReadOnlyList<object> Published(this IDbContextOutbox<InventoryDbContext> outbox) =>
        outbox.ReceivedCalls()
            .Where(c => c.GetMethodInfo().Name == "PublishAsync")
            .Select(c => c.GetArguments()[0]!)
            .ToList();

    /// <summary>The single published message of type <typeparamref name="T"/> (fails the assertion if
    /// there isn't exactly one).</summary>
    public static T PublishedMessage<T>(this IDbContextOutbox<InventoryDbContext> outbox) =>
        outbox.Published().OfType<T>().Single();
}
