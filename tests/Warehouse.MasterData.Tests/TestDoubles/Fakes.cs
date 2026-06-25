using Warehouse.MasterData.Catalog.Application.Abstractions;
using Warehouse.MasterData.Catalog.Domain;
using Warehouse.SharedKernel.Application;

namespace Warehouse.MasterData.Tests.TestDoubles;

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

/// <summary>In-memory <see cref="IProductTypeRepository"/>. Aggregates are mutated by reference, so
/// <see cref="Add"/>/<see cref="Update"/> only need to register the instance.</summary>
internal sealed class FakeProductTypeRepository : IProductTypeRepository
{
    private readonly Dictionary<Sku, ProductType> _store = [];

    public IReadOnlyCollection<ProductType> Saved => _store.Values;

    public void Seed(ProductType product) => _store[product.Sku] = product;

    public Task<ProductType?> GetByIdAsync(Sku id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.GetValueOrDefault(id));

    public void Add(ProductType aggregate) => _store[aggregate.Sku] = aggregate;

    public void Update(ProductType aggregate) => _store[aggregate.Sku] = aggregate;

    public Task<bool> ExistsAsync(Sku sku, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.ContainsKey(sku));

    public Task<bool> ExistsByEanAsync(Ean ean, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.Values.Any(p => p.Ean == ean));
}
