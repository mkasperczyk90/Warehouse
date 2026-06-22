using Warehouse.Warehousing.Inventory.Domain;
using Warehouse.Warehousing.Inventory.Domain.Replicas;

namespace Warehouse.Warehousing.Inventory.Application.Abstractions;

/// <summary>
/// Read/write port for the local product replica (fed by the Catalog's <c>ProductDefined</c> event).
/// Inventory reads it to enforce put-away invariants without a cross-service query (ADR-0003).
/// </summary>
public interface IProductSnapshotRepository
{
    Task<ProductSnapshot?> FindAsync(Sku sku, CancellationToken cancellationToken = default);

    void Add(ProductSnapshot snapshot);

    void Update(ProductSnapshot snapshot);
}
