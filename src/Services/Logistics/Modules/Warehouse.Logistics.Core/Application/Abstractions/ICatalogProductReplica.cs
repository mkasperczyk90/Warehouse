using Warehouse.Logistics.Core.Domain;
using Warehouse.Logistics.Core.Domain.Replicas;

namespace Warehouse.Logistics.Core.Application.Abstractions;

/// <summary>
/// Read/write port for the local Catalog product replica (fed by the <c>ProductDefined</c>
/// integration event). Lets the inbound use cases validate announced SKUs without a cross-service
/// query (ADR-0003).
/// </summary>
public interface ICatalogProductReplica
{
    /// <summary>The replica row for a product code, or <c>null</c> if the catalog never announced it.</summary>
    Task<CatalogProductSnapshot?> FindAsync(ProductCode code, CancellationToken cancellationToken = default);

    /// <summary>Of the supplied codes, the ones not present in the replica (unknown SKUs).</summary>
    Task<IReadOnlyCollection<ProductCode>> FindUnknownAsync(
        IReadOnlyCollection<ProductCode> codes, CancellationToken cancellationToken = default);

    void Add(CatalogProductSnapshot snapshot);

    void Update(CatalogProductSnapshot snapshot);
}
