using NSubstitute;
using Warehouse.SharedKernel.Application;
using Warehouse.SharedKernel.Domain;
using Warehouse.SharedKernel.ValueObjects;
using Warehouse.Warehousing.Topology.Application.Abstractions;
using Warehouse.Warehousing.Topology.Application.Warehouses.EstablishWarehouse;
using Warehouse.Warehousing.Topology.Domain;
using Warehouse.Warehousing.Topology.Infrastructure.Persistence;
using Wolverine.EntityFrameworkCore;
using Xunit;

namespace Warehouse.Warehousing.Tests.Topology;

/// <summary>Counts commits so handler tests can assert the unit of work was flushed once.</summary>
internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveCount { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveCount++;
        return Task.FromResult(1);
    }
}

/// <summary>In-memory <see cref="IWarehouseRepository"/>. Aggregates mutate by reference, so
/// <see cref="Add"/>/<see cref="Update"/> only need to register the instance.</summary>
internal sealed class FakeWarehouseRepository : IWarehouseRepository
{
    private readonly Dictionary<WarehouseCode, WarehouseSite> _store = [];

    public IReadOnlyCollection<WarehouseSite> Saved => _store.Values;

    public void Seed(WarehouseSite site) => _store[site.Code] = site;

    public Task<WarehouseSite?> GetByIdAsync(WarehouseCode id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.GetValueOrDefault(id));

    public void Add(WarehouseSite aggregate) => _store[aggregate.Code] = aggregate;

    public void Update(WarehouseSite aggregate) => _store[aggregate.Code] = aggregate;

    public Task<bool> ExistsAsync(WarehouseCode code, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.ContainsKey(code));
}

/// <summary>Builders for valid topology fixtures, so each test only states what it cares about.</summary>
internal static class Build
{
    public static Address Address() => SharedKernel.ValueObjects.Address.Of("ul. Testowa 1", "Wrocław", "50-001", "PL");

    public static WarehouseSite Warehouse(string code = "WAW1", string name = "Warsaw DC") =>
        WarehouseSite.Establish(WarehouseCode.Of(code), name, Address());

    /// <summary>A warehouse with one room already added (default environment for the type).</summary>
    public static WarehouseSite WarehouseWithRoom(
        string code = "WAW1", string room = "CHLD1", RoomType type = RoomType.ColdRoom)
    {
        var site = Warehouse(code);
        site.AddRoom(RoomCode.Of(room), type);
        return site;
    }

    public static EstablishWarehouseCommand EstablishCommand(string code = "WAW1", string name = "Warsaw DC") =>
        new(code, name, "ul. Testowa 1", "Wrocław", "50-001", "PL");
}

/// <summary>
/// A substitute transactional outbox for handler tests. NSubstitute auto-returns completed tasks for the
/// async members, so <c>PublishAsync</c> + <c>SaveChangesAndFlushMessagesAsync</c> just work; we then read
/// back what was published.
/// </summary>
internal static class TopologyOutbox
{
    public static IDbContextOutbox<TopologyDbContext> Create() =>
        Substitute.For<IDbContextOutbox<TopologyDbContext>>();

    public static IReadOnlyList<object> Published(this IDbContextOutbox<TopologyDbContext> outbox) =>
        outbox.ReceivedCalls()
            .Where(c => c.GetMethodInfo().Name == "PublishAsync")
            .Select(c => c.GetArguments()[0]!)
            .ToList();

    public static T PublishedMessage<T>(this IDbContextOutbox<TopologyDbContext> outbox) =>
        outbox.Published().OfType<T>().Single();
}

/// <summary>Asserts a <see cref="DomainException"/> with a specific stable error code.</summary>
internal static class Expect
{
    public static async Task<DomainException> DomainErrorAsync(string code, Func<Task> act)
    {
        var ex = await Assert.ThrowsAsync<DomainException>(act);
        Assert.Equal(code, ex.ErrorCode);
        return ex;
    }
}
