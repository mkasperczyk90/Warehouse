using Microsoft.EntityFrameworkCore;
using Warehouse.Logistics.Core.Application.Abstractions;
using Warehouse.Logistics.Core.Domain;
using Warehouse.Logistics.Core.Domain.Replicas;

namespace Warehouse.Logistics.Core.Infrastructure.Persistence;

/// <summary>EF Core implementation of the Catalog product replica port.</summary>
internal sealed class CatalogProductReplicaRepository(LogisticsDbContext context) : ICatalogProductReplica
{
    public Task<CatalogProductSnapshot?> FindAsync(ProductCode code, CancellationToken cancellationToken = default) =>
        context.CatalogProducts.FirstOrDefaultAsync(p => p.Code == code, cancellationToken);

    public async Task<IReadOnlyCollection<ProductCode>> FindUnknownAsync(
        IReadOnlyCollection<ProductCode> codes, CancellationToken cancellationToken = default)
    {
        // The replica is small and this is not a hot path; load its codes and diff in memory rather
        // than push a Contains over a value-converted column.
        var known = (await context.CatalogProducts.Select(p => p.Code).ToListAsync(cancellationToken))
            .ToHashSet();

        return codes.Distinct().Where(code => !known.Contains(code)).ToList();
    }

    public void Add(CatalogProductSnapshot snapshot) => context.CatalogProducts.Add(snapshot);

    public void Update(CatalogProductSnapshot snapshot) => context.CatalogProducts.Update(snapshot);
}
