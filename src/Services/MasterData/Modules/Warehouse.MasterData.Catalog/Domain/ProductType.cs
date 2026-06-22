using Warehouse.MasterData.Catalog.Domain.Events;
using Warehouse.SharedKernel.Domain;
using Warehouse.SharedKernel.ValueObjects;

namespace Warehouse.MasterData.Catalog.Domain;

/// <summary>
/// Description archetype (🟦): the product card — what a SKU means. Identified by its SKU
/// (natural key). Physical goods are represented in the Inventory context as batches and
/// quantities, never here.
/// </summary>
public sealed class ProductType : AggregateRoot<Sku>
{
    private readonly List<UnitConversion> _unitConversions = [];

    private ProductType(
        Sku sku,
        string name,
        Ean? ean,
        ProductCategory category,
        Dimensions dimensions,
        Weight unitWeight,
        UnitOfMeasure baseUnit,
        StorageRequirement storage,
        bool isBatchTracked,
        bool hasExpiryDate)
        : base(sku)
    {
        Name = name;
        Ean = ean;
        Category = category;
        Dimensions = dimensions;
        UnitWeight = unitWeight;
        BaseUnit = baseUnit;
        Storage = storage;
        IsBatchTracked = isBatchTracked;
        HasExpiryDate = hasExpiryDate;
    }

    private ProductType()
    {
    }

    public Sku Sku => Id;

    public string Name { get; private set; } = null!;

    public Ean? Ean { get; private set; }

    public ProductCategory Category { get; private set; }

    public Dimensions Dimensions { get; private set; } = null!;

    public Weight UnitWeight { get; private set; }

    public UnitOfMeasure BaseUnit { get; private set; } = null!;

    public StorageRequirement Storage { get; private set; } = null!;

    /// <summary>Batch-tracked products require a batch number on every goods receipt.</summary>
    public bool IsBatchTracked { get; private set; }

    /// <summary>Expiry-dated products are picked FEFO and need an expiry date per batch.</summary>
    public bool HasExpiryDate { get; private set; }

    /// <summary>Alternative units this product can be handled in (e.g. pallets, cartons).</summary>
    public IReadOnlyCollection<UnitConversion> UnitConversions => _unitConversions.AsReadOnly();

    public static ProductType Define(
        Sku sku,
        string name,
        Ean? ean,
        ProductCategory category,
        Dimensions dimensions,
        Weight unitWeight,
        UnitOfMeasure baseUnit,
        StorageRequirement storage,
        bool isBatchTracked,
        bool hasExpiryDate)
    {
        ArgumentNullException.ThrowIfNull(sku);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(dimensions);
        ArgumentNullException.ThrowIfNull(baseUnit);
        ArgumentNullException.ThrowIfNull(storage);

        EnsureStorageMatchesCategory(category, storage);

        // An expiry date only makes sense when we can tell batches apart.
        if (hasExpiryDate && !isBatchTracked)
        {
            throw new DomainException(
                "product_expiry_requires_batches",
                $"Product {sku} declares expiry dates but is not batch-tracked.");
        }

        var product = new ProductType(
            sku, name.Trim(), ean, category, dimensions, unitWeight, baseUnit, storage,
            isBatchTracked, hasExpiryDate);
        product.Raise(new ProductDefined(sku, DateTimeOffset.UtcNow));
        return product;
    }

    public void ChangeStorageRequirement(StorageRequirement storage)
    {
        ArgumentNullException.ThrowIfNull(storage);
        EnsureStorageMatchesCategory(Category, storage);
        if (Storage == storage)
        {
            return;
        }

        Storage = storage;
        Raise(new ProductStorageChanged(Sku, storage, DateTimeOffset.UtcNow));
    }

    public void Rename(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
    }

    /// <summary>Registers an alternative handling unit, e.g. 1 plt = 48 pcs for this SKU.</summary>
    public void AddUnitConversion(UnitOfMeasure unit, decimal factorToBase)
    {
        ArgumentNullException.ThrowIfNull(unit);
        if (unit == BaseUnit)
        {
            throw new DomainException(
                "conversion_base_unit",
                $"{Sku}: cannot add a conversion for the base unit {BaseUnit} itself.");
        }

        if (_unitConversions.Any(c => c.Unit == unit))
        {
            throw new DomainException(
                "conversion_duplicate",
                $"{Sku} already has a conversion for {unit}.");
        }

        _unitConversions.Add(UnitConversion.Of(unit, factorToBase));
    }

    /// <summary>Converts a quantity in any registered unit to the product's base unit.</summary>
    public Quantity ToBaseUnit(Quantity quantity)
    {
        ArgumentNullException.ThrowIfNull(quantity);
        if (quantity.Unit == BaseUnit)
        {
            return quantity;
        }

        var conversion = GetConversion(quantity.Unit);
        return Quantity.Of(quantity.Amount * conversion.FactorToBase, BaseUnit);
    }

    /// <summary>Converts between any two registered units (via the base unit).</summary>
    public Quantity Convert(Quantity quantity, UnitOfMeasure target)
    {
        ArgumentNullException.ThrowIfNull(target);
        var inBase = ToBaseUnit(quantity);
        if (target == BaseUnit)
        {
            return inBase;
        }

        var conversion = GetConversion(target);
        return Quantity.Of(inBase.Amount / conversion.FactorToBase, target);
    }

    private UnitConversion GetConversion(UnitOfMeasure unit) =>
        _unitConversions.SingleOrDefault(c => c.Unit == unit)
        ?? throw new DomainException(
            "conversion_missing",
            $"{Sku} has no conversion for {unit}; base unit is {BaseUnit}.");

    private static void EnsureStorageMatchesCategory(ProductCategory category, StorageRequirement storage)
    {
        switch (category)
        {
            case ProductCategory.Refrigerated or ProductCategory.Frozen when !storage.RequiresColdChain:
                throw new DomainException(
                    "storage_cold_chain_required",
                    $"A {category} product must declare a cold-chain storage requirement.");

            case ProductCategory.Frozen when storage.Temperature is { MaxCelsius: > -15 }:
                throw new DomainException(
                    "storage_frozen_range_invalid",
                    "A frozen product must be stored at -15°C or below.");

            case ProductCategory.Hazardous when !storage.IsHazardous:
                throw new DomainException(
                    "storage_hazmat_required",
                    "A hazardous product must declare a hazardous storage requirement.");
        }
    }
}
