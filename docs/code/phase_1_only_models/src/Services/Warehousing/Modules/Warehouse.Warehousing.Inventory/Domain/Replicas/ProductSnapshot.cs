using Warehouse.SharedKernel.ValueObjects;

namespace Warehouse.Warehousing.Inventory.Domain.Replicas;

/// <summary>
/// Local, minimal replica of the Catalog's product card — only what Inventory needs to
/// enforce its invariants. Updated by ProductDefined/ProductStorageChanged integration
/// events; never queried cross-service. Eventual consistency is an accepted trade-off.
/// </summary>
public sealed class ProductSnapshot
{
    public ProductSnapshot(
        Sku sku,
        UnitOfMeasure baseUnit,
        Weight unitWeight,
        Volume unitVolume,
        TemperatureRange? requiredTemperature,
        bool requiresColdChain,
        bool isHazardous,
        bool isBatchTracked,
        bool hasExpiryDate,
        DateTimeOffset updatedAt)
    {
        ArgumentNullException.ThrowIfNull(sku);
        ArgumentNullException.ThrowIfNull(baseUnit);
        Sku = sku;
        BaseUnit = baseUnit;
        UnitWeight = unitWeight;
        UnitVolume = unitVolume;
        RequiredTemperature = requiredTemperature;
        RequiresColdChain = requiresColdChain;
        IsHazardous = isHazardous;
        IsBatchTracked = isBatchTracked;
        HasExpiryDate = hasExpiryDate;
        UpdatedAt = updatedAt;
    }

    private ProductSnapshot()
    {
    }

    public Sku Sku { get; private set; } = null!;

    public UnitOfMeasure BaseUnit { get; private set; } = null!;

    public Weight UnitWeight { get; private set; }

    public Volume UnitVolume { get; private set; }

    public TemperatureRange? RequiredTemperature { get; private set; }

    public bool RequiresColdChain { get; private set; }

    public bool IsHazardous { get; private set; }

    public bool IsBatchTracked { get; private set; }

    public bool HasExpiryDate { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }
}
