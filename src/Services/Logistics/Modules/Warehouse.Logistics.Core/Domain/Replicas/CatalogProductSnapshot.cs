namespace Warehouse.Logistics.Core.Domain.Replicas;

/// <summary>
/// Local, minimal replica of the Catalog's product card — only what Logistics needs to validate an
/// inbound line (UC-01: "the SKUs exist in the catalog") and to know whether a product is
/// batch-tracked. Updated by the <c>ProductDefined</c> integration event; never queried
/// cross-service (ADR-0003). Eventual consistency is an accepted trade-off.
/// </summary>
public sealed class CatalogProductSnapshot
{
    public CatalogProductSnapshot(ProductCode code, string baseUnit, bool isBatchTracked, DateTimeOffset updatedAt)
    {
        ArgumentNullException.ThrowIfNull(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(baseUnit);
        Code = code;
        BaseUnit = baseUnit;
        IsBatchTracked = isBatchTracked;
        UpdatedAt = updatedAt;
    }

    private CatalogProductSnapshot()
    {
    }

    /// <summary>The catalog SKU as a Logistics <see cref="ProductCode"/> (the replica's identity).</summary>
    public ProductCode Code { get; private set; } = null!;

    public string BaseUnit { get; private set; } = null!;

    public bool IsBatchTracked { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>Applies a later <c>ProductDefined</c>/<c>ProductStorageChanged</c> projection.</summary>
    public void Apply(string baseUnit, bool isBatchTracked, DateTimeOffset updatedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseUnit);
        BaseUnit = baseUnit;
        IsBatchTracked = isBatchTracked;
        UpdatedAt = updatedAt;
    }
}
